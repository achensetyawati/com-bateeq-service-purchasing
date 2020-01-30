﻿using Com.DanLiris.Service.Purchasing.Lib.Helpers;
using Com.DanLiris.Service.Purchasing.Lib.Interfaces;
using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentPurchaseRequestModel;
using Com.DanLiris.Service.Purchasing.Lib.ViewModels.PRMasterValidationReportViewModel;
using Com.Moonlay.Models;
using Com.Moonlay.NetCore.Lib;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Com.DanLiris.Service.Purchasing.Lib.Facades.PRMasterValidationReportFacade
{
    public class PRMasterValidationReportFacade : IPRMasterValidationReportFacade
    {
        private readonly PurchasingDbContext dbContext;
        private readonly DbSet<GarmentPurchaseRequest> dbSet;

        public PRMasterValidationReportFacade(PurchasingDbContext dbContext)
        {
            this.dbContext = dbContext;
            this.dbSet = dbContext.Set<GarmentPurchaseRequest>();
            //this.IdentityService = serviceProvider.GetService<IdentityService>();
        }

        public IQueryable<PRMasterValidationReportViewModel> GetDisplayQuery(string unit, DateTime? dateFrom, DateTime? dateTo, int offset)
        {
            DateTime DateFrom = dateFrom == null ? new DateTime(1970, 1, 1) : (DateTime)dateFrom;
            DateTime DateTo = dateTo == null ? DateTime.Now : (DateTime)dateTo;

            var Query = (from a in dbContext.GarmentPurchaseRequests
                         where a.IsDeleted == false
                               && a.UnitId == (string.IsNullOrWhiteSpace(unit) ? a.UnitId : unit)
                               && a.ShipmentDate.AddHours(offset).Date >= DateFrom.Date
                               && a.ShipmentDate.AddHours(offset).Date <= DateTo.Date

                         select new PRMasterValidationReportViewModel
                         {
                             RO_Number = a.RONo,
                             UnitName = a.UnitName,
                             PRDate = a.Date,
                             DeliveryDate = a.ShipmentDate,
                             SectionName = a.SectionName ,
                             BuyerCode = a.BuyerCode,
                             BuyerName = a.BuyerName,
                             Article = a.Article,
                             StaffName = a.CreatedBy,
                             ValidatedMD = a.IsValidatedMD1 == true ? "SUDAH" : "BELUM",
                             ValidatedPurch = a.IsValidatedPurchasing == true ? "SUDAH" : "BELUM",
                             ValidatedKadiv = a.IsValidatedMD2 == true ? "SUDAH" : "BELUM",
                             ValidatedDate = a.ValidatedMD2Date, 
                         }
                         );
            return Query;
        }

        public Tuple<List<PRMasterValidationReportViewModel>, int> GetDisplayReport(string unit, DateTime? dateFrom, DateTime? dateTo, string Order, int offset)

        {
            var Query = GetDisplayQuery(unit, dateFrom, dateTo, offset);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            if (OrderDictionary.Count.Equals(0))
            {
                Query = Query.OrderBy(b => b.RO_Number).ThenBy(b => b.BuyerCode);
            }
            else
            {
                string Key = OrderDictionary.Keys.First();
                string OrderType = OrderDictionary[Key];
            }

            var data = Query.ToList();
            int TotalData = data.Count();
            
            return Tuple.Create(data, TotalData);
        }

        public MemoryStream GenerateExcel(string unit, DateTime? dateFrom, DateTime? dateTo, int offset)
        {
            var Query = GetDisplayQuery(unit, dateFrom, dateTo, offset);
            Query = Query.OrderBy(b => b.RO_Number).ThenBy(b => b.BuyerCode);
            DataTable result = new DataTable();
            result.Columns.Add(new DataColumn() { ColumnName = "No", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Nomor RO", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Nama Staff", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Konfeksi", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Tgl PR Master", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Seksi", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Kode Brand", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Nama Brand Buyer", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Article", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Tgl Shipment", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Valid Kabag Merchandiser", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Valid Kabag Purchasing", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Valid Kadiv Merchandiser", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Tgl Valid Kadiv Merchandiser", DataType = typeof(String) });

            if (Query.ToArray().Count() == 0)
                result.Rows.Add("", "", "", "", "", "", "", "", "", "", "", "", "", ""); // to allow column name to be generated properly for empty data as template
            else
            {
                var index = 0;
                foreach (var item in Query)
                {
                    index++;

                    string PRMDate = item.PRDate == null ? "-" : item.PRDate.ToOffset(new TimeSpan(offset, 0, 0)).ToString("dd MMM yyyy", new CultureInfo("id-ID"));
                    string ShipDate = item.DeliveryDate == new DateTime(1970, 1, 1) ? "-" : item.DeliveryDate.ToOffset(new TimeSpan(offset, 0, 0)).ToString("dd MMM yyyy", new CultureInfo("id-ID"));
                    string ValidDate = item.ValidatedDate == null ? "-" : item.ValidatedDate.ToOffset(new TimeSpan(offset, 0, 0)).ToString("dd MMM yyyy", new CultureInfo("id-ID"));

                    result.Rows.Add(index, item.RO_Number, item.StaffName ,item.UnitName, PRMDate, item.SectionName, item.BuyerCode, item.BuyerName, item.Article, ShipDate, item.ValidatedMD, item.ValidatedPurch, item.ValidatedKadiv, ValidDate);
                }
            }
            return Excel.CreateExcel(new List<KeyValuePair<DataTable, string>>() { new KeyValuePair<DataTable, string>(result, "Valid PR Maser") }, true);

        }
    }
}
