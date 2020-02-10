using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Zetalex.TableList.Example.Mvc.Core.Models
{
    [Serializable]
    public class ProductModel
    {
        public int ID { get; set; }
        public int WhiteLabelID { get; set; }
        public string Title { get; set; }
        [DisplayFormat(DataFormatString = "{0:0.00}", ApplyFormatInEditMode = true)]
        public decimal Initial { get; set; }
        public bool IsTest { get; set; }
        public List<ProductBlockPrice> BlockPrices { get; set; }
    }

    [Serializable]
    public class ProductBlockPrice : TableListItem
    {
        [TableListHiddenInput]
        public int ID { get; set; }
        [TableListHiddenInput]
        public int ProductID { get; set; }
        [Required]
        [ReadOnly(true)]
        [DisplayFormat(DataFormatString = "{0:0.00}", ApplyFormatInEditMode = true)]
        public decimal Initial { get; set; }
        [DisplayFormat(DataFormatString = "{0:0.00}", ApplyFormatInEditMode = true)]
        public decimal? Renewal { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "License Count must be greater then 0")]
        public int CountFrom { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "License Count must be greater then 0")]
        [DisplayName("Count To")]
        [ReadOnly(true)]
        public int? CountTo { get; set; }
        public bool Default { get; set; }
        public bool xxDefault { get; set; }
    }
}