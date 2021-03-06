﻿using AutoMapper;
using Com.Danliris.Service.Finishing.Printing.Test.Controller.Utils;
using Com.Danliris.Service.Production.Lib.BusinessLogic.Interfaces.Master;
using Com.Danliris.Service.Production.Lib.Models.Master.Instruction;
using Com.Danliris.Service.Production.Lib.Services.IdentityService;
using Com.Danliris.Service.Production.Lib.Services.ValidateService;
using Com.Danliris.Service.Production.Lib.Utilities;
using Com.Danliris.Service.Production.Lib.ViewModels.Master.Instruction;
using Com.Danliris.Service.Production.WebApi.Controllers.v1.Master;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Com.Danliris.Service.Finishing.Printing.Test.Controllers
{
    public class InstructionControllerTest : BaseControllerTest<InstructionController, InstructionModel, InstructionViewModel, IInstructionFacade>
    {
        [Fact]
        public void GetStepVM_WithoutException_ReturnOK()
        {
            var mockFacade = new Mock<IInstructionFacade>();
            mockFacade.Setup(x => x.ReadVM(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new ReadResponse<InstructionModel>(new List<InstructionModel>(), 0, new Dictionary<string, string>(), new List<string>()));

            var mockMapper = new Mock<IMapper>();

            var mockIdentityService = new Mock<IIdentityService>();

            var mockValidateService = new Mock<IValidateService>();

            InstructionController controller = new InstructionController(mockIdentityService.Object, mockValidateService.Object, mockFacade.Object, mockMapper.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
            controller.ControllerContext.HttpContext.Request.Headers["x-timezone-offset"] = $"{It.IsAny<int>()}";

            var response = controller.GetStepVM();
            Assert.Equal((int)HttpStatusCode.OK, GetStatusCode(response));
        }

        [Fact]
        public void GetStepVM_ReadThrowException_ReturnInternalServerError()
        {
            var mockFacade = new Mock<IInstructionFacade>();
            mockFacade.Setup(x => x.ReadVM(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception());

            var mockMapper = new Mock<IMapper>();

            var mockIdentityService = new Mock<IIdentityService>();

            var mockValidateService = new Mock<IValidateService>();

            InstructionController controller = new InstructionController(mockIdentityService.Object, mockValidateService.Object, mockFacade.Object, mockMapper.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
            controller.ControllerContext.HttpContext.Request.Headers["x-timezone-offset"] = $"{It.IsAny<int>()}";

            var response = controller.GetStepVM();
            Assert.Equal((int)HttpStatusCode.InternalServerError, GetStatusCode(response));
        }
    }
}
