﻿using System;

namespace ClassifiedAds.Services.Product.Models
{
    public class ProductModel
    {
        public Guid Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
