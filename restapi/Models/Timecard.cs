using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace restapi.Models
{
    public class Timecard
    {
        public Timecard(int resource)
        {
            Resource = resource;
            UniqueIdentifier = Guid.NewGuid();
            Identity = new TimecardIdentity();
            Lines = new List<AnnotatedTimecardLine>();
            Transitions = new List<Transition> { 
                new Transition(new Entered() { Resource = resource }) 
            };
        }

        public int Resource { get; private set; }
        
        [JsonProperty("id")]
        public TimecardIdentity Identity { get; private set; }

        public TimecardStatus Status { 
            get 
            { 
                return Transitions
                    .OrderByDescending(t => t.OccurredAt)
                    .First()
                    .TransitionedTo;
            } 
        }

        public DateTime Opened;

        [JsonProperty("recId")]
        public int RecordIdentity { get; set; } = 0;

        [JsonProperty("recVersion")]
        public int RecordVersion { get; set; } = 0;

        public Guid UniqueIdentifier { get; set; }

        [JsonIgnore]
        public IList<AnnotatedTimecardLine> Lines { get; set; }

        [JsonIgnore]
        public IList<Transition> Transitions { get; set; }

        public IList<ActionLink> Actions { get => GetActionLinks(); }
    
        [JsonProperty("documentation")]
        public IList<DocumentLink> Documents { get => GetDocumentLinks(); }

        public string Version { get; set; } = "timecard-0.1";

        private IList<ActionLink> GetActionLinks()
        {
            var links = new List<ActionLink>();

            switch (Status)
            {
                case TimecardStatus.Draft:
                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Cancellation,
                        Relationship = ActionRelationship.Cancel,
                        Reference = $"/timesheets/{Identity.Value}/cancellation"
                    });

                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Submittal,
                        Relationship = ActionRelationship.Submit,
                        Reference = $"/timesheets/{Identity.Value}/submittal"
                    });

                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Delete,
                        Relationship = ActionRelationship.Delete,
                        Reference = $"/timesheets/{Identity.Value}/delete"
                    });

                     links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.TimesheetLine,
                        Relationship = ActionRelationship.RecordLine,
                        Reference = $"/timesheets/{Identity.Value}/lines"
                    });
                
                    break;

                case TimecardStatus.Submitted:
                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Cancellation,
                        Relationship = ActionRelationship.Cancel,
                        Reference = $"/timesheets/{Identity.Value}/cancellation"
                    });

                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Rejection,
                        Relationship = ActionRelationship.Reject,
                        Reference = $"/timesheets/{Identity.Value}/rejection"
                    });

                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Approval,
                        Relationship = ActionRelationship.Approve,
                        Reference = $"/timesheets/{Identity.Value}/approval"
                    });

                    break;

                case TimecardStatus.Approved:
                    // terminal state, nothing possible here
                    break;

                case TimecardStatus.Cancelled:
                    // terminal state, nothing possible here

                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Delete,
                        Relationship = ActionRelationship.Delete,
                        Reference = $"/timesheets/{Identity.Value}/delete"
                    });
                    
                    break;
            }

            return links;
        }

        private IList<DocumentLink> GetDocumentLinks()
        {
            var links = new List<DocumentLink>();

            links.Add(new DocumentLink() {
                Method = Method.Get,
                Type = ContentTypes.Transitions,
                Relationship = DocumentRelationship.Transitions,
                Reference = $"/timesheets/{Identity.Value}/transitions"
            });

            if (this.Lines.Count > 0)
            {
                links.Add(new DocumentLink() {
                    Method = Method.Get,
                    Type = ContentTypes.TimesheetLine,
                    Relationship = DocumentRelationship.Lines,
                    Reference = $"/timesheets/{Identity.Value}/lines"
                });
            }

            if (this.Status == TimecardStatus.Submitted)
            {
                links.Add(new DocumentLink() {
                    Method = Method.Get,
                    Type = ContentTypes.Transitions,
                    Relationship = DocumentRelationship.Submittal,
                    Reference = $"/timesheets/{Identity.Value}/submittal"
                });
            }

            return links;
        }

        public AnnotatedTimecardLine AddLine(TimecardLine timecardLine)
        {
            var annotatedLine = new AnnotatedTimecardLine(timecardLine);

            Lines.Add(annotatedLine);

            return annotatedLine;
        }

        public int FindLineIndex(Guid uniqueIdentifier)
        {
            //var annotatedLine = new AnnotatedTimecardLine(timecardLine);

            for (int i = 0; i < Lines.Count; i++) {
                if(Lines[i].UniqueIdentifier == uniqueIdentifier) {
                    return i;
                }
            }

            return -1;
        }

        public AnnotatedTimecardLine ReplaceLine(Guid uniqueIdentifier, TimecardLine timecardLine)
        {
            var annotatedLine = new AnnotatedTimecardLine(timecardLine);

            Lines.RemoveAt(FindLineIndex(uniqueIdentifier));

            Lines.Add(annotatedLine);

            return annotatedLine;
        }

        
        public AnnotatedTimecardLine UpdateLine(Guid uniqueIdentifier, TimecardLine timecardLine)
        {
            var oldLine = Lines[FindLineIndex(uniqueIdentifier)];

            if (timecardLine.Week != oldLine.Week) {
                oldLine.Week = timecardLine.Week;
            }
            if (timecardLine.Year != oldLine.Year) {
                oldLine.Year = timecardLine.Year;
            }
            if (timecardLine.Day != oldLine.Day) {
                oldLine.Day = timecardLine.Day;
            }
            if (timecardLine.Hours != oldLine.Hours) {
                oldLine.Hours = timecardLine.Hours;
            }
            if (timecardLine.Project != oldLine.Project) {
                oldLine.Project = timecardLine.Project;
            }

            return oldLine;
        }

    }
}