﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TrueFitProjectTracker.Models.Cache;
using TrueFitProjectTracker.Models.Dashboard;

namespace TrueFitProjectTracker.Factories.Dashboard {
    public class TasksFactory : JiraFactory {
	/// <summary>
	/// The path in the Jira API to the listing of all issues associated
	/// with a particular project.
	/// </summary>
		private const string API_ISSUES_LIST = "search?jql=project=";

	/// <summary>
	/// The name of the Jira Task field which associates a task with a 
	/// particular epic.
	/// </summary>
		private const string EPIC_FIELD_NAME = "Epic Link";

	/// <summary>
	/// A <c>DateTime</c> object holding the Unix Epoch. This is used as
	/// a default value when Jira is unable to supply a date.
	/// </summary>
		private DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	/// <summary>
	/// A reference to the factory which will supply the field IDs for the
	/// Epic and Sprint fields.
	/// </summary>
		private FieldFactory Fields;


	/// <summary>
	/// This object will contain the raw data returned from Jira, which can 
	/// be accessed from another function or child class again, if needed
	/// </summary>
		protected Object Issues;

	/// <summary>
	/// A listing of sprints associated with a particular project. This
	/// simply takes the <c>TasksFactor.Sprints</c> Dictionary<> and converts
	/// it to a List<> of SprintModel classes.
	/// </summary>
	/// 
	/// <see cref="TasksFactory.Sprints"/>
		public List<SprintModel> List {
			get {
				return Sprints.Values.ToList<SprintModel>();
			}
		}

	/// <summary>
	/// The name of the Jira Task field which associates a task with a 
	/// particular sprint.
	/// </summary>
		private const string SPRINT_FIELD_NAME = "Sprint";

	/// <summary>
	/// The listing of all sprints available for a particular project. Each
	/// sprint object can be referenced by its name within the Dictionary<>.
	/// </summary>
		private Dictionary<string, SprintModel> Sprints;

	/// <summary>
	/// A flat List<> of all available tasks, given a minimal amount of 
	/// processing.
	/// </summary>
		protected List<TaskModel> Tasks;

		public TasksFactory(string projectKey) : base() {
			Fields = new FieldFactory(Jira);
			Sprints = new Dictionary<string, SprintModel>();
			Tasks = new List<TaskModel>();

		//Fetch the data from the Jira API
			fetchTasks(projectKey);
		}

		private void addToSprint(string sprintData, TaskModel tm) {
			string name = sprintData == null ? "Unassigned" : getSprintName(sprintData);

		//A new sprint may need to be created
			if (sprintData == null && !Sprints.ContainsKey(name)) {
				Sprints[name] = new SprintModel();
				Sprints[name].CompleteDate = Epoch;
				Sprints[name].EndDate = Epoch;
				Sprints[name].Name = name;
				Sprints[name].StartDate = Epoch;
				Sprints[name].State = "FUTURE";
				Sprints[name].Tasks = new List<TaskModel>();
			} else if (!Sprints.ContainsKey(name)) {
				Sprints[name] = createSprint(sprintData);
			}

			Sprints[name].Tasks.Add(tm);
		}

		private SprintModel createSprint(string sprintData) {
			SprintModel sm = new SprintModel();

		//This is a comma delimited list
			string[] parts = sprintData.Split(',');
			string[] small;

		//Completion date
			small = parts[5].Split('=');

			if (small[1] == "<null>") {
				sm.CompleteDate = Epoch;
			} else {
				sm.CompleteDate = DateTime.Parse(small[1]);
			}

		//End date
			small = parts[4].Split('=');

			if (small[1] == "<null>") {
				sm.EndDate = Epoch;
			} else {
				sm.EndDate = DateTime.Parse(small[1]);
			}

		//Name
			small = parts[2].Split('=');
			sm.Name = small[1];

		//Start date
			small = parts[3].Split('=');

			if (small[1] == "<null>") {
				sm.StartDate = Epoch;
			} else {
				sm.StartDate = DateTime.Parse(small[1]);
			}

		//State
			small = parts[1].Split('=');
			sm.State = small[1];

		//Tasks
			sm.Tasks = new List<TaskModel>();

			return sm;
		}

		private void fetchTasks(string projectKey) {
		//Find the Epic and Sprint field IDs
			string epicField, sprintField;

			if (Fields.List[0].Name == EPIC_FIELD_NAME) {
				epicField = Fields.List[0].ID;
				sprintField = Fields.List[1].ID;
			} else {
				epicField = Fields.List[1].ID;
				sprintField = Fields.List[0].ID;
			}	

		//Fetch all of the tasks for a particular project
			Issues = Jira.RPC(API_ISSUES_LIST + HttpUtility.UrlEncode("\"" + projectKey + "\""));
			Dictionary<String, Object> parent = Issues as Dictionary<String, Object>;

		//Iterate over all of the issues
			IEnumerable<Object> issueList = parent["issues"] as IEnumerable<Object>;

			foreach(Dictionary<String, Object> issue in issueList) {
				TaskModel tm = new TaskModel();

			//Parse the data from each of the Jira fields, as supplied by the API
				Dictionary<String, Object> fields = issue["fields"] as Dictionary<String, Object>;

			//Creation date
				string created = fields["created"] as string;

				if (created == null) {
					tm.Created = Epoch;
				} else {
					tm.Created = DateTime.Parse(fields["created"] as string);
				}				

			//Description
				tm.Description = fields["description"] as string;

			//Due date
				string due = fields["duedate"] as string;

				if (due == null) {
					tm.DueDate = Epoch;
				} else {
					tm.DueDate = DateTime.Parse(fields["duedate"] as string);
				}

			//Epic
				tm.Epic = fields[epicField] as string;

			//Issue
				Dictionary<String, Object> issueDetails = fields["issuetype"] as Dictionary<String, Object>;
				tm.Issue = issueDetails["name"] as string;

			//Name
				tm.Name = fields["summary"] as string;

			//Priority
				Dictionary<String, Object> priorityDetails = fields["priority"] as Dictionary<String, Object>;
				tm.Priority = priorityDetails["name"] as string;

			//Progress
				Dictionary<String, Object> progressDetails = fields["progress"] as Dictionary<String, Object>;
				tm.Progress = new ProgressModel();
				tm.Progress.Committed = (int)progressDetails["progress"];
				tm.Progress.Expected = (int)progressDetails["total"];

				if (progressDetails.ContainsKey("percent")) {
					tm.Progress.Percent = (int)progressDetails["percent"];

					if (tm.Progress.Percent > 100) { // If Committed > Expected, may be greater than 100%
						tm.Progress.Percent = 100;
					}
				} else {
					if (tm.Progress.Expected > 0) {
						tm.Progress.Percent = Convert.ToInt32(tm.Progress.Committed / tm.Progress.Expected);
					} else {
						tm.Progress.Percent = 0;
					}
				}

			//Resolution
				if (fields["resolution"] == null) {
					fields["resolution"] = null;
				} else {
					Dictionary<String, Object> resolutionDetails = fields["resolution"] as Dictionary<String, Object>;
					tm.Resolution = resolutionDetails["name"] as string;
				}

			//Resolution date
				string resolution = fields["resolutiondate"] as string;

				if(resolution == null) {
					tm.ResolutionDate = Epoch;
				} else {
					tm.ResolutionDate = DateTime.Parse(fields["resolutiondate"] as string);
				}

			//Status
				Dictionary<String, Object> statusDetails = fields["status"] as Dictionary<String, Object>;
				tm.Status = statusDetails["name"] as string;

			//Add this task to the appropriate sprint
				object[] sprint = fields[sprintField] as object[];

				if (sprint == null) {
					addToSprint(null, tm);
				} else {
					addToSprint(sprint[0] as string, tm);
				}

			//Add the tasks to a flat array of tasks
				Tasks.Add(tm);
			}

		//Sort the tasks
			foreach (SprintModel sm in List) {
				sm.Tasks.Sort((x, y) => x.Created.CompareTo(y.Created));
				Tasks.Sort((x, y) => x.Created.CompareTo(y.Created));
			}
		}

		private string getSprintName(string sprintData) {
		//This is a comma delimited list
			string[] parts = sprintData.Split(',');
			string[] small;

		//Name
			small = parts[2].Split('=');
			return small[1];
		}
    }
}