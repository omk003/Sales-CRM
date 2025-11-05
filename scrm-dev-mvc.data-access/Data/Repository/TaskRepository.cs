using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.data_access.Data.Repository.IRepository;
using scrm_dev_mvc.DataAccess.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.data_access.Data.Repository
{
    public class TaskRepository: Repository<scrm_dev_mvc.Models.Task>, ITaskRepository
    {
        private readonly ApplicationDbContext _db;
        public TaskRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(scrm_dev_mvc.Models.Task task)
        {
            _db.Tasks.Update(task);
            //var objFromDb = _db.Tasks.FirstOrDefault(u => u.Id == task.Id);
            //if (objFromDb != null)
            //{
            //    objFromDb.Status = task.Status;
            //    //objFromDb.Title = task.Title;
            //    //objFromDb.Description = task.Description;
            //    //objFromDb.DueDate = task.DueDate;
            //    //objFromDb.PriorityId = task.PriorityId;
            //    //objFromDb.AssignedToUserId = task.AssignedToUserId;
            //    //objFromDb.UpdatedAt = DateTime.Now;
            //}

        }
    }
}
