﻿using Com.Danliris.Service.Finishing.Printing.Lib.Models.Daily_Operation;
using Com.Danliris.Service.Finishing.Printing.Lib.Models.Kanban;
using Com.Danliris.Service.Finishing.Printing.Lib.ViewModels.Daily_Operation;
using Com.Danliris.Service.Production.Lib;
using Com.Danliris.Service.Production.Lib.Services.IdentityService;
using Com.Danliris.Service.Production.Lib.Utilities.BaseClass;
using Com.Moonlay.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Com.Danliris.Service.Finishing.Printing.Lib.BusinessLogic.Implementations.DailyOperation
{
    public class DailyOperationLogic : BaseLogic<DailyOperationModel>
    {
        private const string UserAgent = "production-service";
        private DailyOperationBadOutputReasonsLogic DailyOperationBadOutputReasonsLogic;
        private readonly DbSet<KanbanModel> DbSetKanban;
        private readonly ProductionDbContext DbContext;

        public DailyOperationLogic(DailyOperationBadOutputReasonsLogic dailyOperationBadOutputReasonsLogic, IIdentityService identityService, ProductionDbContext dbContext) : base(identityService, dbContext)
        {
            this.DbContext = dbContext;
            this.DbSetKanban = DbContext.Set<KanbanModel>();
            this.DailyOperationBadOutputReasonsLogic = dailyOperationBadOutputReasonsLogic;
        }

        public override void CreateModel(DailyOperationModel model)
        {

            if (model.Type == "output")
            {
                foreach (DailyOperationBadOutputReasonsModel item in model.BadOutputReasons)
                {
                    EntityExtension.FlagForCreate(item, IdentityService.Username, UserAgent);
                }
            }

            this.SetKanbanCreate(model);

            model.Kanban = null;
            model.Machine = null;

            base.CreateModel(model);
        }

        public override async Task DeleteModel(int id)
        {
            var model = await ReadModelById(id);

            if (model.Type == "output")
            {
                // string flag = "delete";
                foreach (var item in model.BadOutputReasons)
                {
                    EntityExtension.FlagForDelete(item, IdentityService.Username, UserAgent);
                }
                this.SetKanbanDelete(model);
            }

            model.Kanban = null;
            model.Machine = null;
            EntityExtension.FlagForDelete(model, IdentityService.Username, UserAgent, true);
            DbSet.Update(model);
        }

        public override async Task UpdateModelAsync(int id, DailyOperationModel model)
        {
            if (model.Type == "output")
            {
                // string flag = "update";
                HashSet<int> detailId = DailyOperationBadOutputReasonsLogic.DataId(id);
                foreach (var itemId in detailId)
                {
                    DailyOperationBadOutputReasonsModel data = model.BadOutputReasons.FirstOrDefault(prop => prop.Id.Equals(itemId));
                    if (data == null)
                        await DailyOperationBadOutputReasonsLogic.DeleteModel(itemId);
                    else
                    {
                        await DailyOperationBadOutputReasonsLogic.UpdateModelAsync(itemId, data);
                    }

                    foreach (DailyOperationBadOutputReasonsModel item in model.BadOutputReasons)
                    {
                        if (item.Id == 0)
                            DailyOperationBadOutputReasonsLogic.CreateModel(item);
                    }
                }
                // this.UpdateKanban(model, flag);
                this.SetKanbanUpdate(model);
            }

            model.Kanban = null;
            model.Machine = null;

            EntityExtension.FlagForUpdate(model, IdentityService.Username, UserAgent);
            DbSet.Update(model);
        }

        public override Task<DailyOperationModel> ReadModelById(int id)
        {
            return DbSet.Include(d => d.Machine)
                .Include(d => d.Kanban)
                    .ThenInclude(d => d.Instruction)
                        .ThenInclude(d => d.Steps)
                .Include(d => d.BadOutputReasons)
                .FirstOrDefaultAsync(d => d.Id.Equals(id) && d.IsDeleted.Equals(false));
        }

        //public void UpdateKanban(DailyOperationModel model, string flag)
        //{
        //	KanbanModel kanban = this.DbSetKanban.Where(k => k.Id.Equals(model.KanbanId)).SingleOrDefault();

        //	int currentStepIndex = (flag == "create" ? kanban.CurrentStepIndex += 1 : flag == "update" ? kanban.CurrentStepIndex : kanban.CurrentStepIndex -= 1);

        //	kanban.CurrentQty = model.GoodOutput != null ? (double)model.GoodOutput : 0;
        //	kanban.CurrentStepIndex = currentStepIndex;
        //	kanban.GoodOutput = model.GoodOutput != null ? (double)model.GoodOutput : 0;
        //	kanban.BadOutput = model.BadOutput != null ? (double)model.GoodOutput : 0;

        //	EntityExtension.FlagForUpdate(kanban, IdentityService.Username, UserAgent);
        //	DbSetKanban.Update(kanban);
        //}

        public void SetKanbanCreate(DailyOperationModel model)
        {
            var selectedKanban = this.DbSetKanban.Where(kanban => kanban.Id == model.KanbanId).SingleOrDefault();
            // var selectedKanbanInstruction = this.DbContext.KanbanInstructions.Where(kanbanInstruction => kanbanInstruction.KanbanId == selectedKanban.Id).SingleOrDefault();

            if (model.Type.ToUpper() == "INPUT")
            {
                selectedKanban.CurrentStepIndex += 1;
                selectedKanban.CurrentQty = model.Input.GetValueOrDefault();
                model.KanbanStepIndex = selectedKanban.CurrentStepIndex;
            }
            else if (model.Type.ToUpper() == "OUTPUT")
            {
                model.KanbanStepIndex = selectedKanban.CurrentStepIndex;
                selectedKanban.CurrentQty = model.GoodOutput.GetValueOrDefault() + model.BadOutput.GetValueOrDefault();
                selectedKanban.GoodOutput = model.GoodOutput.GetValueOrDefault();
                selectedKanban.BadOutput = model.BadOutput.GetValueOrDefault();
            }

            DbContext.Kanbans.Update(selectedKanban);
        }

        public void SetKanbanUpdate(DailyOperationModel model)
        {
            var selectedKanban = this.DbSetKanban.Where(kanban => kanban.Id == model.KanbanId).SingleOrDefault();
            // var selectedKanbanInstruction = this.DbContext.KanbanInstructions.Where(kanbanInstruction => kanbanInstruction.KanbanId == selectedKanban.Id).SingleOrDefault();

            if (model.Type.ToUpper() == "INPUT")
            {
                selectedKanban.CurrentQty = model.Input.GetValueOrDefault();
            }
            else if (model.Type.ToUpper() == "OUTPUT")
            {
                selectedKanban.CurrentQty = model.GoodOutput.GetValueOrDefault() + model.BadOutput.GetValueOrDefault();
                selectedKanban.GoodOutput = model.GoodOutput.GetValueOrDefault();
                selectedKanban.BadOutput = model.BadOutput.GetValueOrDefault();
            }

            DbContext.Kanbans.Update(selectedKanban);
        }

        public void SetKanbanDelete(DailyOperationModel model)
        {
            var selectedKanban = this.DbSetKanban.Where(kanban => kanban.Id == model.KanbanId).SingleOrDefault();

            var previousState = GetPreviousState(model);

            if (previousState != null)
            {
                if (previousState.Type.ToUpper() == "INPUT")
                {
                    selectedKanban.CurrentQty = previousState.Input.GetValueOrDefault();
                }
                else if (previousState.Type.ToUpper() == "OUTPUT")
                {
                    selectedKanban.CurrentQty = previousState.GoodOutput.GetValueOrDefault() + previousState.BadOutput.GetValueOrDefault();
                    selectedKanban.GoodOutput = previousState.GoodOutput.GetValueOrDefault();
                    selectedKanban.BadOutput = previousState.BadOutput.GetValueOrDefault();
                }
            }

            DbContext.Kanbans.Update(selectedKanban);
        }

        public DailyOperationModel GetPreviousState(DailyOperationModel model)
        {
            if (model.Type.ToUpper() == "INPUT")
            {
                return DbSet.Where(dailyOperation => dailyOperation.KanbanId == model.KanbanId && dailyOperation.KanbanStepIndex == model.KanbanStepIndex - 1 && model.Type.ToUpper() == "OUTPUT").SingleOrDefault();
            }
            else
            {
                return DbSet.Where(dailyOperation => dailyOperation.KanbanId == model.KanbanId && dailyOperation.KanbanStepIndex == model.KanbanStepIndex && model.Type.ToUpper() == "INPUT").SingleOrDefault();
            }
        }

        //public HashSet<int> hasInput(DailyOperationViewModel vm)
        //{
        //	return new HashSet<int>(DbSet.Where(d => d.Kanban.Id == vm.Kanban.Id && d.Type == vm.Type && d.StepId == vm.Step.StepId).Select(d => d.Id));
        //}

        public DailyOperationModel GetInputDataForCurrentOutput(DailyOperationViewModel vm)
        {
            return DbSet.FirstOrDefault(s => s.KanbanId == vm.Kanban.Id && s.Type.ToLower() == "input" && s.KanbanStepIndex == vm.Kanban.CurrentStepIndex.GetValueOrDefault());
        }

        public bool ValidateCreateOutputDataCheckCurrentInput(DailyOperationViewModel vm)
        {
            return !DbSet.Any(s => s.KanbanId == vm.Kanban.Id && s.Type.ToLower() == "input" && s.KanbanStepIndex == vm.Kanban.CurrentStepIndex.GetValueOrDefault());
        }

        public bool ValidateCreateOutputDataCheckDuplicate(DailyOperationViewModel vm)
        {
            return DbSet.Any(s => s.KanbanId == vm.Kanban.Id && s.Type.ToLower() == "output" && s.KanbanStepIndex == vm.Kanban.CurrentStepIndex.GetValueOrDefault());
        }

        public bool ValidateCreateInputDataCheckPreviousOutput(DailyOperationViewModel vm)
        {
            if (vm.Kanban.CurrentStepIndex == 0)
                return false;

            return !DbSet.Any(s => s.KanbanId == vm.Kanban.Id && s.Type.ToLower() == "output" && (s.KanbanStepIndex == vm.Kanban.CurrentStepIndex.GetValueOrDefault()));
        }

        public bool ValidateCreateInputDataCheckDuplicate(DailyOperationViewModel vm)
        {

            return DbSet.Any(s => s.KanbanId == vm.Kanban.Id && s.Type.ToLower() == "input" && s.KanbanStepIndex == (vm.Kanban.CurrentStepIndex.GetValueOrDefault() + 1));
        }

        //public async Task<int> ETLKanbanStepIndex(int page)
        //{

        //	var groupedData = DbSet
        //		.Select(x => new DailyOperationModel()
        //		{
        //			Id = x.Id,
        //			KanbanId = x.KanbanId,
        //			StepProcess = x.StepProcess,
        //			DateInput = x.DateInput,
        //			TimeInput = x.TimeInput,
        //			DateOutput = x.DateOutput,
        //			TimeOutput = x.TimeOutput,
        //			Type = x.Type,
        //			KanbanStepIndex = x.KanbanStepIndex
        //		}).GroupBy(s => new { s.KanbanId, s.StepProcess }).Where(x => x.Count() > 2).OrderBy(x => x.Key.KanbanId);


        //	int dd = groupedData.Count();
        //	var kanbanStepData = DbContext.KanbanSteps.Include(x => x.Instruction)
        //		.Select(x => new KanbanStepModel()
        //		{
        //			Id = x.Id,
        //			Instruction = new KanbanInstructionModel()
        //			{
        //				Id = x.Instruction.Id,
        //				KanbanId = x.Instruction.KanbanId
        //			},
        //			InstructionId = x.InstructionId,
        //			StepIndex = x.StepIndex,
        //			Process = x.Process
        //		});
        //	int index = 0;
        //	int result = 0;
        //	using (var transaction = DbContext.Database.BeginTransaction())
        //	{
        //		foreach (var item in groupedData)
        //		//foreach (var item in groupedData.GroupBy(x => x.KanbanId).OrderBy(x => x.Key).Skip((page - 1) * 5000).Take(5000))
        //		{
        //			var dataInput = item.Where(x => x.Type.ToLower() == "input").OrderBy(x => x.DateInput).ThenBy(x => x.TimeInput);
        //			var dataOutput = item.Where(x => x.Type.ToLower() == "output").OrderBy(x => x.DateOutput).ThenBy(x => x.TimeOutput);
        //			//var data = item.OrderBy(x => x.CreatedUtc).ThenBy(x => x.Id);
        //			var steps = kanbanStepData.Where(x => x.Instruction.KanbanId == item.Key.KanbanId && x.Process == item.Key.StepProcess).OrderBy(x => x.StepIndex);

        //			foreach(var daily in dataInput)
        //			{
        //				int idx = dataInput.ToList().FindIndex(x => x.Id == daily.Id);
        //				var kanbanStep = steps.ToList().ElementAtOrDefault(idx);
        //				var model = await DbSet.FirstOrDefaultAsync(x => x.Id == daily.Id);
        //				if (kanbanStep != null)
        //				{

        //					model.KanbanStepIndex = kanbanStep.StepIndex;

        //				}
        //				else
        //				{
        //					model.KanbanStepIndex = 0;

        //				}
        //				index++;
        //				Debug.WriteLine(index);

        //				if (index % 10000 == 0)
        //				{
        //					result += await DbContext.SaveChangesAsync();
        //				}
        //			}

        //			foreach (var daily in dataOutput)
        //			{
        //				int idx = dataOutput.ToList().FindIndex(x => x.Id == daily.Id);
        //				var kanbanStep = steps.ToList().ElementAtOrDefault(idx);
        //				var model = await DbSet.FirstOrDefaultAsync(x => x.Id == daily.Id);
        //				if (kanbanStep != null)
        //				{

        //					model.KanbanStepIndex = kanbanStep.StepIndex;

        //				}
        //				else
        //				{
        //					model.KanbanStepIndex = 0;
        //				}
        //				index++;
        //				Debug.WriteLine(index);

        //				if (index % 10000 == 0)
        //				{
        //					result += await DbContext.SaveChangesAsync();
        //				}
        //			}
        //			//foreach (var daily in data)
        //			//{
        //			//	int idx = data.Where(x => x.StepProcess == daily.StepProcess).ToList().FindIndex(x => x.StepProcess == daily.StepProcess);
        //			//	var kanbanStep = steps.Where(x => x.Process == daily.StepProcess).ToList().ElementAtOrDefault(idx);
        //			//	if (kanbanStep != null)
        //			//	{
        //			//		var model = await DbSet.FirstOrDefaultAsync(x => x.Id == daily.Id);
        //			//		model.KanbanStepIndex = kanbanStep.StepIndex;
        //			//		index++;
        //			//		Debug.WriteLine(index);

        //			//		if (index % 10000 == 0)
        //			//		{
        //			//			result += await DbContext.SaveChangesAsync();
        //			//		}
        //			//	}

        //			//}
        //		}
        //		result += await DbContext.SaveChangesAsync();
        //		transaction.Commit();
        //	}

        //	return result;
        //}
    }
}