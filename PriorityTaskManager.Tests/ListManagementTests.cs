using System;
using System.IO;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using Xunit;

namespace PriorityTaskManager.Tests
{
    public class ListManagementTests : IDisposable
    {
        private readonly TaskManagerService _service;
        private readonly string _uniqueTestId = Guid.NewGuid().ToString();
        private string TestTasksFile => $"test_tasks_{_uniqueTestId}.json";
        private string TestListsFile => $"test_lists_{_uniqueTestId}.json";

        public ListManagementTests()
        {
            File.Delete(TestTasksFile);
            File.Delete(TestListsFile);
            var urgencyService = new UrgencyService();
            _service = new TaskManagerService(urgencyService, TestTasksFile, TestListsFile);
        }

        public void Dispose()
        {
            File.Delete(TestTasksFile);
            File.Delete(TestListsFile);
        }

        [Fact]
        public void TaskManagerService_ShouldCreateDefaultGeneralList_OnFirstLoad()
        {
            var lists = _service.GetAllLists();

            Assert.Single(lists);
            Assert.Equal("General", lists.First().Name);
        }

        [Fact]
        public void AddList_ShouldIncreaseListCount()
        {
            var newList = new TaskList { Name = "Work" };
            _service.AddList(newList);

            Assert.Contains(newList, _service.GetAllLists());
        }

        [Fact]
        public void AddList_ShouldThrowInvalidOperationException_ForDuplicateName()
        {
            var newList = new TaskList { Name = "Work" };
            _service.AddList(newList);

            Assert.Throws<InvalidOperationException>(() => _service.AddList(new TaskList { Name = "work" }));
        }

        [Fact]
        public void GetAllTasks_ShouldOnlyReturnTasksFromSpecifiedList()
        {
            var workList = new TaskList { Name = "Work" };
            var homeList = new TaskList { Name = "Home" };
            _service.AddList(workList);
            _service.AddList(homeList);

            var workTask = new TaskItem { Title = "Work Task", ListName = "Work" };
            var homeTask = new TaskItem { Title = "Home Task", ListName = "Home" };
            _service.AddTask(workTask);
            _service.AddTask(homeTask);

            var workTasks = _service.GetAllTasks("Work");

            Assert.Contains(workTask, workTasks);
            Assert.DoesNotContain(homeTask, workTasks);
        }

        [Fact]
        public void DeleteList_ShouldRemoveListAndAllAssociatedTasks()
        {
            var toDeleteList = new TaskList { Name = "ToDelete" };
            var toKeepList = new TaskList { Name = "ToKeep" };
            _service.AddList(toDeleteList);
            _service.AddList(toKeepList);

            var task1 = new TaskItem { Title = "Task 1", ListName = "ToDelete" };
            var task2 = new TaskItem { Title = "Task 2", ListName = "ToKeep" };
            _service.AddTask(task1);
            _service.AddTask(task2);

            _service.DeleteList("ToDelete");

            Assert.Null(_service.GetListByName("ToDelete"));
            Assert.Empty(_service.GetAllTasks("ToDelete"));
            Assert.Contains(task2, _service.GetAllTasks("ToKeep"));
        }

        [Fact]
        public void UpdateList_ShouldChangeSortOption()
        {
            var list = new TaskList { Name = "Work", SortOption = SortOption.Default };
            _service.AddList(list);

            list.SortOption = SortOption.Alphabetical;
            _service.UpdateList(list);

            var updatedList = _service.GetListByName("Work");
            Assert.Equal(SortOption.Alphabetical, updatedList.SortOption);
        }
    }
}
