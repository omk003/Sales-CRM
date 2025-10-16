using Microsoft.AspNetCore.Mvc;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace scrm_dev_mvc.Controllers
{
    public class DealController : Controller
    {
        public IActionResult KanbanBoard()
        {
            // Example Users
            var aliceId = Guid.NewGuid();
            var bobId = Guid.NewGuid();
            var charlieId = Guid.NewGuid();
            var danaId = Guid.NewGuid();
            var eveId = Guid.NewGuid();
            var frankId = Guid.NewGuid();

            var users = new List<User>
            {
                new User { Id = aliceId, FirstName = "Alice" },
                new User { Id = bobId, FirstName = "Bob" },
                new User { Id = charlieId, FirstName = "Charlie" },
                new User { Id = danaId, FirstName = "Dana" },
                new User { Id = eveId, FirstName = "Eve" },
                new User { Id = frankId, FirstName = "Frank" }
            };

            // Example Deal Stages
            var stages = new List<Stage>
            {
                new Stage { Id = 1, Name = "Prospecting" },
                new Stage { Id = 2, Name = "Qualification" },
                new Stage { Id = 3, Name = "Proposal" },
                new Stage { Id = 4, Name = "Negotiation" },
                new Stage { Id = 5, Name = "Won" },
                new Stage { Id = 6, Name = "Lost" }
            };

            // Example Deals
            var deals = new List<Deal>
            {
                new Deal
                {
                    Id = 1,
                    OwnerId = aliceId,
                    StageId = 1,
                    Stage = stages[0],
                    Value = 50000,
                    Owner = users.First(u => u.Id == aliceId),
                    CreatedAt = DateTime.Now.AddDays(-10)
                },
                new Deal
                {
                    Id = 2,
                    OwnerId = bobId,
                    StageId = 2,
                    Stage = stages[1],
                    Value = 42000,
                    Owner = users.First(u => u.Id == bobId),
                    CreatedAt = DateTime.Now.AddDays(-8)
                },
                new Deal
                {
                    Id = 3,
                    OwnerId = charlieId,
                    StageId = 3,
                    Stage = stages[2],
                    Value = 120000,
                    Owner = users.First(u => u.Id == charlieId),
                    CreatedAt = DateTime.Now.AddDays(-6)
                },
                new Deal
                {
                    Id = 4,
                    OwnerId = danaId,
                    StageId = 4,
                    Stage = stages[3],
                    Value = 30000,
                    Owner = users.First(u => u.Id == danaId),
                    CreatedAt = DateTime.Now.AddDays(-4)
                },
                new Deal
                {
                    Id = 5,
                    OwnerId = eveId,
                    StageId = 5,
                    Stage = stages[4],
                    Value = 150000,
                    Owner = users.First(u => u.Id == eveId),
                    CreatedAt = DateTime.Now.AddDays(-2),
                    CloseDate = DateTime.Now.AddDays(-1)
                },
                new Deal
                {
                    Id = 6,
                    OwnerId = frankId,
                    StageId = 6,
                    Stage = stages[5],
                    Value = 25000,
                    Owner = users.First(u => u.Id == frankId),
                    CreatedAt = DateTime.Now.AddDays(-1),
                    CloseDate = DateTime.Now
                }
            };

            // Group Deals by Stage Name
            var dealsByStage = stages.ToDictionary(
                stage => stage.Name,
                stage => deals.Where(d => d.Stage != null && d.Stage.Name == stage.Name).ToList()
            );

            var vm = new KanbanBoardViewModel
            {
                DealStages = stages.Select(s => s.Name).ToList(),
                DealsByStage = dealsByStage
            };

            return View(vm);
        }
    }
}