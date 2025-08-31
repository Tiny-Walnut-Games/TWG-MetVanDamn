using UnityEngine;

namespace LivingDevAgent.Editor.Modules
	{
	// ⚠ Intent ⚠ - @jmeyer1980
	// This class is for Taskmaster use, it is not intended for manual use.
	// This is why the CreateAssetMenu attribute is commented out.
	// While the TimeCardSO is used to track time spent on tasks, the TaskSO
	// is used to define and manage the tasks themselves. As an example, these tasks
	// are what I would consider to be billable: Time cards are internal. Tasks are external.
	//                          💎 Diamonds are forever 💎
	// [CreateAssetMenu(fileName = "TaskData", menuName = "Scriptable Objects/TaskData")]
	public class TaskData : ScriptableObject
		{
		// ⚠ Intent ⚠ - @jmeyer1980
		// This class represents a task that can be tracked and managed by Taskmaster.
		// It contains a name, description, and status for the task.
		// It also contains a reference to a TimeCardData, which is used to track time spent on the task.
		[HideInInspector] public string TaskName;
		[HideInInspector] public string taskDescription;
		[HideInInspector] public bool isCompleted;
		[HideInInspector] public TimeCardData timeCard;
		// Additional properties and methods can be added as needed to manage tasks.
		// Unlike TimeCardData, the TaskData is intended to be accessed and manipulated,
		// not directly, but via Taskmaster API calls. Taskmaster being the... master.
		// This means we're not going to be parsing it directly in this class.
		// Taskmaster reports to the faculty directly, not the other way around.😉
		[HideInInspector] public bool isFinished;
		[HideInInspector] public bool isCanceled;
		[HideInInspector] public System.DateTime createdAt;
		[HideInInspector] public System.DateTime completedAt;
		[HideInInspector] public System.DateTime canceledAt;
		[HideInInspector] public string assignedTo;
		[HideInInspector] public int priorityLevel; // 1 (highest) to 5 (lowest)
		[HideInInspector] public string [ ] tags; // e.g., "bug", "
		[HideInInspector] public string [ ] comments; // e.g., "Started working on this task."
		[HideInInspector] public string [ ] attachments; // e.g., "screenshot.png"
		[HideInInspector] public string [ ] subtasks; // e.g., "Design UI", "Implement feature"
		[HideInInspector] public string [ ] relatedRefs; // e.g., "TaskID123", "Issue#456", "PR#789", etc.
		[HideInInspector] public string [ ] dependencies; // e.g., "TaskID321", "Issue#654", "PR#987", etc.
		[HideInInspector] public string [ ] blockers; // e.g., "Waiting for design approval."
		[HideInInspector] public string [ ] watchers; // e.g., "user1", "user2", etc.
		[HideInInspector] public string [ ] history; // e.g., "Task created.", "Status changed to In Progress."
		[HideInInspector] public string [ ] customFields; // e.g., "Field1: Value1", "Field2: Value2"
		[HideInInspector] public string [ ] auditLogs; // e.g., "User1 changed status to Completed."
		[HideInInspector] public string [ ] notifications; // e.g., "Task assigned to User1."
		[HideInInspector] public string [ ] reminders; // e.g., "Reminder set for tomorrow."
		[HideInInspector] public string [ ] timeEstimates; // e.g., "2 hours", "30 minutes"
		[HideInInspector] public string [ ] timeSpent; // e.g., "1 hour", "15 minutes"
		[HideInInspector] public string [ ] billingCodes; // e.g., "BILL-123", "BILL-456"

		// Create a new TaskData instance
		public static TaskData CreateTask (string name, string description, string assignedUser, int priority)
			{
			TaskData newTask = CreateInstance<TaskData>();
			newTask.TaskName = name; // Fixed: Use proper field name
			newTask.taskDescription = description;
			newTask.isCompleted = false;
			newTask.isFinished = false;
			newTask.isCanceled = false;
			newTask.createdAt = System.DateTime.Now;
			newTask.assignedTo = assignedUser;
			newTask.priorityLevel = priority;
			newTask.tags = new string [ ] { };
			newTask.comments = new string [ ] { };
			newTask.attachments = new string [ ] { };
			newTask.subtasks = new string [ ] { };
			newTask.relatedRefs = new string [ ] { };
			newTask.dependencies = new string [ ] { };
			newTask.blockers = new string [ ] { };
			newTask.watchers = new string [ ] { };
			newTask.history = new string [ ] { $"Task created at {newTask.createdAt}" };
			newTask.customFields = new string [ ] { };
			newTask.auditLogs = new string [ ] { };
			newTask.notifications = new string [ ] { };
			newTask.reminders = new string [ ] { };
			newTask.timeEstimates = new string [ ] { };
			newTask.timeSpent = new string [ ] { };
			newTask.billingCodes = new string [ ] { };
			// Initialize the TimeCardData for this task
			TimeCardData timeCardInstance = CreateInstance<TimeCardData>();
			timeCardInstance.StartTimeCard(newTask);
			newTask.timeCard = timeCardInstance;
			return newTask;
			}

		// Mark the task as completed
		public void CompleteTask ()
			{
			if (!this.isCompleted && !this.isCanceled)
				{
				this.isCompleted = true;
				this.isFinished = true;
				this.completedAt = System.DateTime.Now;
				this.timeCard.EndTimeCard();
				this.history = this.AppendToArray(this.history, $"Task completed at {this.completedAt}");
				}
			else
				{
				Debug.LogWarning("TaskData: Attempted to complete a task that is already completed or canceled.");
				}
			}

		// Cancel the task
		public void CancelTask ()
			{
			if (!this.isCompleted && !this.isCanceled)
				{
				this.isCanceled = true;
				this.isFinished = true;
				this.canceledAt = System.DateTime.Now;
				this.history = this.AppendToArray(this.history, $"Task canceled at {this.canceledAt}");
				}
			else
				{
				Debug.LogWarning("TaskData: Attempted to cancel a task that is already completed or canceled.");
				}
			}

		// Helper method to append a string to a string array
		private string [ ] AppendToArray (string [ ] array, string newItem)
			{
			string [ ] newArray = new string [ array.Length + 1 ];
			array.CopyTo(newArray, 0);
			newArray [ array.Length ] = newItem;
			return newArray;
			}
		}
	}
