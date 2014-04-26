﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Atlassian.Jira;
using TrueFitProjectTracker.ViewModels;

namespace TrueFitProjectTracker.Models
{
    public class BugModel
    {
        public BugModel()
        {
            title = "Agile Scrum";
        }

        public BugModel(ProjectViewModel project, Jira jira)
        {
            title = "Agile Scrum";
        }

        public string title { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Reporter { get; set; }


    }
}
