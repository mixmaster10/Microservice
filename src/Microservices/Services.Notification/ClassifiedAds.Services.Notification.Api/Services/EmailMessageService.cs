using ClassifiedAds.Application;
using ClassifiedAds.CrossCuttingConcerns.OS;
using ClassifiedAds.Domain.Events;
using ClassifiedAds.Domain.Infrastructure.MessageBrokers;
using ClassifiedAds.Infrastructure.Notification.Email;
using ClassifiedAds.Services.Notification.DTOs;
using ClassifiedAds.Services.Notification.Entities;
using ClassifiedAds.Services.Notification.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClassifiedAds.Services.Notification.Services
{
    public class EmailMessageService : CrudService<EmailMessage>, IEmailMessageService
    {
        private readonly ILogger _logger;
        private readonly IEmailMessageRepository _repository;
        private readonly IMessageSender<EmailMessageCreatedEvent> _emailMessageCreatedEventSender;
        private readonly IEmailNotification _emailNotification;
        private readonly IDateTimeProvider _dateTimeProvider;

        public EmailMessageService(ILogger<EmailMessageService> logger,
            IEmailMessageRepository repository,
            IMessageSender<EmailMessageCreatedEvent> emailMessageCreatedEventSender,
            IDomainEvents domainEvents,
            IEmailNotification emailNotification,
            IDateTimeProvider dateTimeProvider)
            : base(repository, domainEvents)
        {
            _logger = logger;
            _repository = repository;
            _emailMessageCreatedEventSender = emailMessageCreatedEventSender;
            _emailNotification = emailNotification;
            _dateTimeProvider = dateTimeProvider;
        }

        public Task CreateEmailMessageAsync(EmailMessageDTO emailMessage)
        {
            return AddOrUpdateAsync(new EmailMessage
            {
                From = emailMessage.From,
                Tos = emailMessage.Tos,
                CCs = emailMessage.CCs,
                BCCs = emailMessage.BCCs,
                Subject = emailMessage.Subject,
                Body = emailMessage.Body,
            });
        }

        public async Task<int> ResendEmailMessagesAsync()
        {
            var dateTime = _dateTimeProvider.OffsetNow.AddMinutes(-1);

            var messages = _repository.GetAll()
                .Where(x => x.SentDateTime == null && x.RetriedCount < 3)
                .Where(x => (x.RetriedCount == 0 && x.CreatedDateTime < dateTime) || (x.RetriedCount != 0 && x.UpdatedDateTime < dateTime))
                .ToList();

            if (messages.Any())
            {
                foreach (var email in messages)
                {
                    await _emailMessageCreatedEventSender.SendAsync(new EmailMessageCreatedEvent { Id = email.Id });

                    email.RetriedCount++;

                    await _repository.AddOrUpdateAsync(email);
                    await _repository.UnitOfWork.SaveChangesAsync();
                }
            }
            else
            {
                _logger.LogInformation("No email to resend.");
            }

            return messages.Count;
        }

        public async Task SendEmailMessageAsync(Guid id)
        {
            var emailMessage = _repository.GetAll().FirstOrDefault(x => x.Id == id);
            if (emailMessage != null && !emailMessage.SentDateTime.HasValue)
            {
                try
                {
                    await _emailNotification.SendAsync(new EmailMessageDTO
                    {
                        From = emailMessage.From,
                        Tos = emailMessage.Tos,
                        CCs = emailMessage.CCs,
                        BCCs = emailMessage.BCCs,
                        Subject = emailMessage.Subject,
                        Body = emailMessage.Body,
                    });

                    _repository.UpdateSent(emailMessage.Id);
                }
                catch (Exception ex)
                {
                    _repository.UpdateFailed(emailMessage.Id, Environment.NewLine + Environment.NewLine + ex.ToString());
                }
            }
        }
    }
}
