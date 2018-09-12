﻿using AutoMapper;
using Com.Danliris.Service.Finishing.Printing.Lib.BusinessLogic.Facades.MonitoringEvent;
using Com.Danliris.Service.Finishing.Printing.Lib.ViewModels.Master.Machine;
using Com.Danliris.Service.Finishing.Printing.Lib.ViewModels.Monitoring_Specification_Machine;
using Com.Danliris.Service.Production.Lib.Services.IdentityService;
using Com.Danliris.Service.Production.WebApi.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Com.Danliris.Service.Sales.WebApi.Controllers
{
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/finishing-printing/monitoring-event-reports")]
    [Authorize]
    public class MonitoringEventReportController : Controller
    {
        private string ApiVersion = "1.0.0";
        private readonly MonitoringEventReportFacade _facade;
        private readonly IMapper mapper;
        //private readonly IdentityService identityService;
        public MonitoringEventReportController(MonitoringEventReportFacade facade, IMapper mapper)//, IdentityService identityService)
        {
            _facade = facade;
            this.mapper = mapper;
            //this.identityService = identityService;
        }

        [HttpGet]
        public IActionResult GetReportAll(string machineId, string machineEventId, string productionOrderNumber, DateTime? dateFrom, DateTime? dateTo, int page, int size, string Order = "{}")
        {
            int offset = Convert.ToInt32(Request.Headers["x-timezone-offset"]);
            string accept = Request.Headers["Accept"];
            if (page == 0)
            {
                page = 1;
                size = 25;
            }
            try
            {

                var data = _facade.GetReport(machineId, machineEventId, productionOrderNumber, dateFrom, dateTo, page, size, Order, offset);

                return Ok(new
                {
                    apiVersion = ApiVersion,
                    data = data.Item1,
                    info = new { total = data.Item2 },
                    message = General.OK_MESSAGE,
                    statusCode = General.OK_STATUS_CODE
                });
            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                    .Fail();
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }
        }

        [HttpGet("download")]
        public IActionResult GetXlsAll(string no, string buyerCode, string comodityCode, DateTime? dateFrom, DateTime? dateTo)
        {

            try
            {
                byte[] xlsInBytes;
                int offset = Convert.ToInt32(Request.Headers["x-timezone-offset"]);
                DateTime DateFrom = dateFrom == null ? new DateTime(1970, 1, 1) : Convert.ToDateTime(dateFrom);
                DateTime DateTo = dateTo == null ? DateTime.Now : Convert.ToDateTime(dateTo);

                var xls = _facade.GenerateExcel(no, buyerCode, comodityCode, dateFrom, dateTo, offset);

                string filename = String.Format("Laporan Monitoring Event - {0}.xlsx", DateTime.UtcNow.ToString("ddMMyyyy"));

                xlsInBytes = xls.ToArray();
                var file = File(xlsInBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
                return file;

            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                    .Fail();
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }
        }

        [HttpGet("by-machine")]
        public IActionResult ByMachine(string keyword, int machineId)
        {
            var Data = _facade.ReadByMachine(keyword, machineId);

            var newData = mapper.Map<List<MachineEventViewModel>>(Data);

            List<object> listData = new List<object>();
            listData.AddRange(
                newData.AsQueryable().Select(s => new
                {
                    s.Id,
                    s.No,
                    s.Name,
                    s.Code,
                    s.LastModifiedUtc,
                }).ToList()
            );

            return Ok(new
            {
                apiVersion = ApiVersion,
                statusCode = General.OK_STATUS_CODE,
                message = General.OK_MESSAGE,
                data = listData,
                info = new Dictionary<string, object>
                {
                    { "count", listData.Count },
                },
            });
        }

        [HttpGet("monitoringSpecMachine")]
        public IActionResult Get(int id, string productionOrderNumber, string dateTime)
        {
            var m = DateTime.ParseExact(dateTime, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            try
            {
                var model = _facade.ReadMonitoringSpecMachine(id, productionOrderNumber, Convert.ToDateTime(dateTime));
                var viewModel = mapper.Map<MonitoringSpecificationMachineViewModel>(model);

                return Ok(new
                {
                    apiVersion = ApiVersion,
                    statusCode = General.OK_STATUS_CODE,
                    message = General.OK_MESSAGE,
                    data = viewModel,
                });
            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                    .Fail();
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }
        }
    }
}