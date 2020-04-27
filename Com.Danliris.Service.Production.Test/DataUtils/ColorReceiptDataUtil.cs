﻿using Com.Danliris.Service.Finishing.Printing.Lib.BusinessLogic.Facades.ColorReceipt;
using Com.Danliris.Service.Finishing.Printing.Lib.Models.ColorReceipt;
using Com.Danliris.Service.Finishing.Printing.Test.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Com.Danliris.Service.Finishing.Printing.Test.DataUtils
{
    public class ColorReceiptDataUtil : BaseDataUtil<ColorReceiptFacade, ColorReceiptModel>
    {
        public ColorReceiptDataUtil(ColorReceiptFacade facade) : base(facade)
        {
        }
        public override ColorReceiptModel GetNewData()
        {
            return new ColorReceiptModel()
            {
                ColorName = "Test",
                TechnicianId = 1,
                TechnicianName = "a",
                ColorCode = "Code"
            };
        }
    }
}
