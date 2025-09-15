using System;
using UnityEngine;

namespace LivingDevAgent.Editor.Modules
	{
	// ⚠ Intent ⚠ - @jmeyer1980
	// This class is for Taskmaster use, it is not intended for manual use.
	// This is why the CreateAssetMenu attribute is commented out.
	// [CreateAssetMenu(fileName = "TimeCardData", menuName = "Scriptable Objects/TimeCardData")]
	public class TimeCardData : ScriptableObject
		{
		// ⚠ Intent ⚠ - @jmeyer1980
		// The time card is a rather unique concept, despite the name.
		// It is not meant to be a record of time worked, but rather a record of time spent on a task.
		// While it could definitely be used for time tracking and billing, that is not its primary purpose.
		// It contains a reference to a TaskData, which is the task that the time card is associated with.
		// It also contains a start time and an end time, which are used to calculate the
		// duration of time spent on the task.
		// The duration is calculated as the difference between the end time and the start time.
		// The duration is stored as a float in hours, but it can be easily converted to
		// minutes or seconds if needed.
		private TaskData taskName;
		[HideInInspector] public int Sessions { get; set; } // Number of sessions logged for this time card
		private DateTime startTime;
		private DateTime endTime;
		private float DurationInHours => (float)(endTime - startTime).TotalHours;
		private float DurationInMinutes => (float)(endTime - startTime).TotalMinutes;
		private float DurationInSeconds => (float)(endTime - startTime).TotalSeconds;
		private bool IsOngoing => endTime == default;

		// 🎯 CORE FEATURE: Completion tracking separate from ongoing status
		// Used for time tracking analytics and task completion reporting
		private bool IsCompleted => endTime != default && taskName != null;

		// 🎯 FIXED: Initialize lastModified and update it when timecard changes
		private DateTime lastModified = DateTime.Now;

		// This information is then used to generate a report for the task,
		// using a private string to hold the parsed collection of data from this time card.
		// This can be requested by Taskmaster, or other API, via public method(s) for reporting purposes.
		private string reportData;

		public TaskData GetTask()
			{
			return taskName;
			}

		public int GetSessionCount()
			{
			return Sessions;
			}

		public DateTime GetStartTime()
			{
			return startTime;
			}

		public DateTime GetEndTime()
			{
			return endTime;
			}

		public float GetDurationInHours()
			{
			return DurationInHours;
			}

		public float GetDurationInMinutes()
			{
			return DurationInMinutes;
			}

		public float GetDurationInSeconds()
			{
			return DurationInSeconds;
			}

		public bool GetIsOngoing()
			{
			return IsOngoing;
			}

		public bool GetIsCompleted()
			{
			return IsCompleted;
			}

		public void StartTimeCard(TaskData associatedTask)
			{
			taskName = associatedTask;
			startTime = DateTime.Now;
			endTime = default;
			reportData = string.Empty;

			// 🎯 FIXED: Update lastModified when starting timecard
			lastModified = DateTime.Now;
			}

		public void EndTimeCard()
			{
			if (IsOngoing)
				{
				endTime = DateTime.Now;
				GenerateReportData();

				// 🎯 FIXED: Update lastModified when ending timecard
				lastModified = DateTime.Now;
				}
			else
				{
				Debug.LogWarning("TimeCardData: Attempted to end a time card that is not ongoing.");
				}
			}

		private void GenerateReportData()
			{
			reportData = $"Task: {taskName.name}\n" +
						 $"Start Time: {startTime}\n" +
						 $"End Time: {endTime}\n" +
						 $"Duration: {DurationInHours:F2} hours ({DurationInMinutes:F2} minutes, {DurationInSeconds:F2} seconds)";
			}

		public string GetReportData()
			{
			if (string.IsNullOrEmpty(reportData))
				{
				Debug.LogWarning("TimeCardData: Report data is empty. Ensure the time card has been ended.");
				}
			return reportData;
			}

		internal DateTime GetLastModified()
			{
			return lastModified;
			}

		public void IncrementSessionCount()
			{
			Sessions++;

			// 🎯 FIXED: Update lastModified when incrementing sessions
			lastModified = DateTime.Now;
			}

		// 💡 Expansion Opportunity 💡 - @jmeyer1980
		// Additional properties and methods can be added as needed to manage time cards.
		// Like deletion, pausing, resuming, etc.
		}
	}
