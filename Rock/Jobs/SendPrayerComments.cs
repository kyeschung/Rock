﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;

using Quartz;

using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;

namespace Rock.Jobs
{
    /// <summary>
    /// A Rock job to send a specified email template to all prayer request authors with a list of comments from those who have prayed for their requests.
    /// </summary>
    [DisplayName( "Send Prayer Request Comments Notifications" )]
    [Category( "Prayer" )]
    [Description( "Sends an email notification to all prayer request authors with a list of comments from those who have prayed for their requests." )]
    [DisallowConcurrentExecution]

    #region Job Attributes

    [SystemEmailField( "System Email",
        "The system email template to be used for the notifications.",
        required: true,
        order: 0,
        key: AttributeKey.SystemEmail,
        defaultSystemEmailGuid: SystemGuid.SystemEmail.PRAYER_REQUEST_COMMENTS_NOTIFICATION )]
    [CategoryField( "Prayer Categories",
        "A category filter for the Prayer Requests to include. If not specified, all categories will be included.",
        EntityType = typeof( Rock.Model.PrayerRequest ),
        Order = 1,
        IsRequired = false,
        AllowMultiple = true,
        Key = AttributeKey.PrayerCategories )]
    [BooleanField( "Include Child Categories",
        "Should Prayer Requests in child categories of the selected filter categories be included.",
        Order = 2,
        Key = AttributeKey.IncludeChildCategories )]
    [BooleanField( "Save Communications",
        "Should the notifications be recorded as Communication entries?",
        Order = 3,
        Key = AttributeKey.SaveCommunications )]

    #endregion

    public class SendPrayerComments : IJob
    {
        #region Attribute Keys

        /// <summary>
        /// Keys to use for Workflow Attributes
        /// </summary>
        private static class AttributeKey
        {
            /// <summary>
            /// The unique identifier (GUID) of the Data View that will supply the list of entities.
            /// </summary>
            public const string SystemEmail = "SystemEmail";

            /// <summary>
            /// The unique identifier (Guid) of the Workflow that will be launched for each item in the list of entities.
            /// </summary>
            public const string PrayerCategories = "PrayerCategories";
            /// <summary>
            /// The unique identifier (Guid) of the Workflow that will be launched for each item in the list of entities.
            /// </summary>
            public const string IncludeChildCategories = "IncludeChildCategories";
            /// <summary>
            /// The unique identifier (Guid) of the Workflow that will be launched for each item in the list of entities.
            /// </summary>
            public const string SaveCommunications = "SaveCommunications";

        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SendPrayerComments"/> class.
        /// </summary>
        public SendPrayerComments()
        {
            //
        }

        #endregion

        #region Properties

        private TaskLog _Log = new TaskLog();
        private List<int> _CategoryIdList;
        private List<Note> _PrayerComments;
        private List<PrayerRequest> _PrayerRequests;
        private List<RockEmailMessage> _Notifications;

        /// <summary>
        /// Returns the log device for this job.
        /// </summary>
        public TaskLog Log
        {
            get
            {
                return _Log;
            }
        }

        /// <summary>
        /// The unique identifier of the system email template used to create the notification emails.
        /// </summary>
        public Guid? SystemEmailTemplateGuid { get; set; }

        /// <summary>
        /// The list of categories containing the prayer requests to be processed.
        /// If not specified, all prayer requests will be processed.
        /// </summary>
        public List<Guid> CategoryGuidList { get; set; }
        public bool IncludeChildCategories { get; set; }

        /// <summary>
        /// Should a communication history record be created for the notifications sent by this job?
        /// </summary>
        public bool CreateCommunicationRecord { get; set; }

        /// <summary>
        /// The unique identifier used to store and retrieve persisted settings for this job.
        /// </summary>
        public string SystemSettingsId { get; set; }

        /// <summary>
        /// The collection of prayer requests that match the filter conditions for this job.
        /// </summary>
        public List<PrayerRequest> PrayerRequests
        {
            get => _PrayerRequests;
        }

        /// <summary>
        /// The collection of prayer request comments that match the filter conditions for this job.
        /// </summary>
        public List<Note> PrayerComments
        {
            get => _PrayerComments;
        }

        /// <summary>
        /// The collection of notifications that are prepared and ready to be sent.
        /// </summary>
        public List<RockEmailMessage> Notifications
        {
            get => _Notifications;
        }

        /// <summary>
        /// The start of the period in which comments will be notified.
        /// </summary>
        public DateTime? StartDate;

        /// <summary>
        /// The end of the period in which comments will be notified.
        /// </summary>
        public DateTime? EndDate;

        #endregion

        /// <summary>
        /// Retrieve the job settings from the context provided by the Quartz scheduler.
        /// </summary>
        /// <param name="context"></param>
        private void GetSettingsFromJobContext( IJobExecutionContext context )
        {
            Log.LogVerbose( $"Reading configuration from job execution context..." );

            var dataMap = context.JobDetail.JobDataMap;

            this.SystemEmailTemplateGuid = dataMap.Get( AttributeKey.SystemEmail ).ToString().AsGuid();

            this.CategoryGuidList = dataMap.Get( AttributeKey.PrayerCategories ).ToString().SplitDelimitedValues().AsGuidList();

            this.IncludeChildCategories = dataMap.Get( AttributeKey.IncludeChildCategories ).ToString().AsBoolean();

            this.CreateCommunicationRecord = dataMap.Get( AttributeKey.SaveCommunications ).ToString().AsBoolean();

            this.SystemSettingsId = context.JobDetail.Description;

            // Get the persisted settings from the previous run, and calculate the appropriate time period.
            var lastRunDate = Rock.Web.SystemSettings.GetValue( GetSystemSettingsKey() ).AsDateTime();

            // If this is the first run, set the start date to the beginning of the previous day.
            if ( !lastRunDate.HasValue )
            {
                lastRunDate = RockDateTime.Now.Date.AddDays( -1 );
            }

            this.StartDate = lastRunDate.Value.AddSeconds( 1 );
            this.EndDate = RockDateTime.Now;
        }

        private string GetSystemSettingsKey()
        {
            return string.Format( "core-prayercommentsdigest-lastrundate-{0}", this.SystemSettingsId );
        }

        /// <summary>
        /// Load the prayer requests and comments data using the current settings.
        /// </summary>
        public void LoadPrayerRequests()
        {
            try
            {
                var rockContext = new RockContext();

                GetPrayerCategories( rockContext );

                GetPrayerRequests( rockContext );
            }
            catch ( Exception ex )
            {
                throw new Exception( "Processing failed with an unexpected error.\n" + ex.Message );
            }
        }

        /// <summary>
        /// Populates the Notifications collection with pending notifications for the current prayer requests, in preparation for sending.
        /// Use the SendNotifications() method to send the messages.
        /// </summary>
        public void PrepareNotifications()
        {
            try
            {
                if ( _PrayerRequests == null )
                {
                    this.LoadPrayerRequests();
                }

                var rockContext = new RockContext();

                PrepareNotifications( rockContext );
            }
            catch ( Exception ex )
            {
                throw new Exception( "Prepare Notifications failed.\n" + ex.Message );
            }
        }

        /// <summary>
        /// Sends notifications for the current prayer requests.
        /// </summary>
        public void SendNotifications()
        {
            try
            {
                if ( _PrayerRequests == null )
                {
                    this.LoadPrayerRequests();
                }

                if ( _Notifications == null )
                {
                    this.PrepareNotifications();
                }

                var rockContext = new RockContext();

                SendNotifications( rockContext );
            }
            catch ( Exception ex )
            {
                throw new Exception( "Processing failed with an unexpected error.\n" + ex.Message );
            }
        }

        /// <summary>
        /// Execute the job using the settings supplied by the specified Quartz scheduler job context.
        /// </summary>
        /// <param name="context">The context.</param>
        public virtual void Execute( IJobExecutionContext context )
        {
            Log.LogProgress( $"Job Started: SendPrayerComments" );

            GetSettingsFromJobContext( context );

            this.LoadPrayerRequests();

            this.SendNotifications();

            var resultMessage = _Log.GetLastMessage( TaskLog.TaskLogMessage.MessageTypeSpecifier.Progress );

            if ( resultMessage != null )
            {
                context.Result = resultMessage.Message;
            }

            var errors = _Log.Messages.Where( x => x.MessageType == TaskLog.TaskLogMessage.MessageTypeSpecifier.Error ).ToList();

            if ( errors.Any() )
            {
                var sb = new StringBuilder();

                sb.AppendLine();
                sb.Append( string.Format( "Error Details: ", errors.Count() ) );

                errors.ForEach( e => { sb.AppendLine(); sb.Append( e.Message ); } );

                string errorMessage = sb.ToString();

                context.Result += errorMessage;

                var exception = new Exception( errorMessage );

                var context2 = HttpContext.Current;

                ExceptionLogService.LogException( exception, context2 );

                throw exception;
            }

            // Save settings to be used for the next job execution.
            Rock.Web.SystemSettings.SetValue( GetSystemSettingsKey(), this.EndDate.ToISO8601DateString() );

            Log.LogProgress( $"Job Completed: SendPrayerComments" );
        }

        /// <summary>
        /// Retrieve and validate the prayer categories.
        /// </summary>
        /// <param name="rockContext"></param>
        private void GetPrayerCategories( RockContext rockContext )
        {
            var categoryService = new CategoryService( rockContext );

            _CategoryIdList = null;

            if ( CategoryGuidList != null
                 && CategoryGuidList.Any() )
            {
                _CategoryIdList = GetCategoryIdList( this.CategoryGuidList, categoryService );

                if ( _CategoryIdList.Any() )
                {
                    _Log.LogError( "Category List is invalid. No matching Categories found." );
                }
            }
        }

        /// <summary>
        /// Prepare notifications for Prayer Requests that have comments added within the specified time period.
        /// </summary>
        /// <param name="rockContext"></param>
        private void PrepareNotifications( RockContext rockContext )
        {
            _Notifications = new List<RockEmailMessage>();

            if ( !SystemEmailTemplateGuid.HasValue )
            {
                _Log.LogError( "Send notifications failed. A System Email Template must be specified.", "Send Notifications" );

                return;
            }

            int processedCount = 0;
            int errorCount = 0;

            var defaultMergeFields = Lava.LavaHelper.GetCommonMergeFields( null );

            foreach ( var prayerRequest in _PrayerRequests )
            {
                Person person = null;

                if ( prayerRequest.RequestedByPersonAlias != null )
                {
                    person = prayerRequest.RequestedByPersonAlias.Person;
                }

                if ( person == null || !person.IsEmailActive || person.Email.IsNullOrWhiteSpace() || person.EmailPreference == EmailPreference.DoNotEmail )
                {
                    _Log.LogWarning( "Notification not sent. Person does not have an email address or Email Preference is \"No Email\"." );
                    continue;
                }

                var comments = _PrayerComments.Where( x => x.EntityId == prayerRequest.Id ).OrderBy( x => x.CreatedDateTime ).ToList();

                if ( !comments.Any() )
                {
                    _Log.LogVerbose( "No comments found for this prayer request." );
                    continue;
                }

                var mergeFields = new Dictionary<string, object>( defaultMergeFields );

                mergeFields.Add( "Person", person );
                mergeFields.Add( "FirstName", prayerRequest.FirstName );
                mergeFields.Add( "LastName", prayerRequest.LastName );
                mergeFields.Add( "Email", prayerRequest.Email );
                mergeFields.Add( "PrayerRequest", prayerRequest );
                mergeFields.Add( "Comments", comments );

                var recipient = new RockEmailMessageRecipient( person, mergeFields );

                var recipients = new List<RockEmailMessageRecipient> { recipient };

                var errors = new List<string>();

                var emailMessage = new RockEmailMessage( SystemEmailTemplateGuid.Value );

                emailMessage.SetRecipients( recipients );

                _Log.LogVerbose( $"Preparing notification for \"{ person.FullName }\" ({ prayerRequest.Email })... " );

                emailMessage.CreateCommunicationRecord = this.CreateCommunicationRecord;

                _Notifications.Add( emailMessage );
            }

            _Log.LogProgress( $"{ processedCount } notifications processed, { errorCount } failed." );
        }

        /// <summary>
        /// Send notifications for Prayer Requests that have comments added within the specified time period.
        /// </summary>
        /// <param name="rockContext"></param>
        private void SendNotifications( RockContext rockContext )
        {
            if ( _Notifications == null )
            {
                this.PrepareNotifications( rockContext );
            }

            int processedCount = 0;
            int errorCount = 0;

            var errors = new List<string>();

            foreach ( var emailMessage in _Notifications )
            {
                var recipient = emailMessage.GetRecipients().FirstOrDefault();

                _Log.LogVerbose( $"Sending notification to \"{ recipient.Name }\"..." );

                emailMessage.CreateCommunicationRecord = this.CreateCommunicationRecord;

                emailMessage.Send( out errors );

                processedCount++;

                if ( errors.Any() )
                {
                    errorCount++;

                    foreach ( var errorMessage in errors )
                    {
                        _Log.LogError( errorMessage, "SendNotification" );
                    }
                }
            }

            _Log.LogProgress( $"{ processedCount } notifications processed, { errorCount } failed." );
        }

        /// <summary>
        /// Retrieve the Prayer Requests and associated Notes that have reportable activity.
        /// </summary>
        /// <param name="rockContext"></param>
        private void GetPrayerRequests( RockContext rockContext )
        {
            Log.LogVerbose( $"Job Configuration:" );
            Log.LogVerbose( $"SystemEmailTemplateGuid = { this.SystemEmailTemplateGuid }" );

            if ( this.CategoryGuidList != null
                 && this.CategoryGuidList.Any() )
            {
                Log.LogVerbose( $"CategoryGuidList = [{ this.CategoryGuidList.AsDelimited( "," ) }]" );
                Log.LogVerbose( $"IncludeChildCategories = { this.IncludeChildCategories }" );
            }

            Log.LogVerbose( $"CreateCommunicationRecord = { this.CreateCommunicationRecord }" );
            Log.LogVerbose( $"Report Period = { this.StartDate } to { this.EndDate }" );

            var prayerRequestService = new PrayerRequestService( rockContext );

            // Get Prayer Requests with comments enabled.
            var prayerRequestQuery = prayerRequestService.Queryable().Where( x => x.AllowComments.HasValue && x.AllowComments.Value );

            // Filter by Categories.
            if ( _CategoryIdList != null )
            {
                prayerRequestQuery = prayerRequestQuery.Where( a => a.CategoryId.HasValue
                                                                && ( _CategoryIdList.Contains( a.CategoryId.Value ) ) );
            }

            var prayerRequestIdList = prayerRequestQuery.Select( a => a.Id );

            // Get the Comments associated with the filtered Prayer Requests and created within the specified timeframe.
            var noteTypeService = new NoteTypeService( rockContext );

            var noteType = noteTypeService.Get( Rock.SystemGuid.NoteType.PRAYER_COMMENT.AsGuid() );

            var noteService = new NoteService( rockContext );

            var prayerCommentsQuery = noteService.GetByNoteTypeId( noteType.Id );

            // Filter Comments by Entity and exclude if marked as Private.
            prayerCommentsQuery = prayerCommentsQuery.Where( a => a.EntityId.HasValue && prayerRequestIdList.Contains( a.EntityId.Value ) && !a.IsPrivateNote );

            // Filter Comments by Date Range.
            if ( StartDate.HasValue )
            {
                DateTime startDate = StartDate.Value.Date;

                prayerCommentsQuery = prayerCommentsQuery.Where( a => a.CreatedDateTime.HasValue && a.CreatedDateTime.Value >= startDate );
            }

            if ( EndDate.HasValue )
            {
                // Add one day in order to include everything up to the end of the selected datetime.
                var endDate = EndDate.Value.Date.AddDays( 1 );

                prayerCommentsQuery = prayerCommentsQuery.Where( a => a.CreatedDateTime.HasValue && a.CreatedDateTime.Value < endDate );
            }

            // Retrieve and store the comments.
            _PrayerComments = prayerCommentsQuery.OrderByDescending( n => n.CreatedDateTime ).ToList();

            // Retrieve and store the set of prayer requests having at least one comment within the filtered date range.
            var prayerRequestWithCommentIdList = prayerCommentsQuery.Select( x => x.EntityId );

            _PrayerRequests = prayerRequestQuery.Where( x => prayerRequestWithCommentIdList.Contains( x.Id ) ).ToList();
        }

        /// <summary>
        /// Returns an enumerable collection of <see cref="Rock.Model.Category">Category</see> that are descendants of a <see cref="Rock.Model.Category" />
        /// </summary>
        /// <param name="parentCategoryGuid">The parent category unique identifier.</param>
        /// <returns>
        /// A collection of <see cref="Rock.Model.Category" /> entities that are descendants of the provided parent <see cref="Rock.Model.Category" />.
        /// </returns>
        public List<int> GetCategoryIdList( IEnumerable<Guid> parentCategoryGuidList, CategoryService categoryService )
        {
            var categoryIdList = new List<int>();

            foreach ( var parentCategoryGuid in parentCategoryGuidList )
            {
                var parentCategory = categoryService.Get( parentCategoryGuid );

                // If the parent category does not exist, or it is a child of a previously added category, ignore it.
                if ( parentCategory == null
                     || categoryIdList.Contains( parentCategory.Id ) )
                {
                    continue;
                }

                categoryIdList.Add( parentCategory.Id );

                if ( this.IncludeChildCategories )
                {
                    var descendantIdList = categoryService.GetAllDescendents( parentCategory.Id ).Select( x => x.Id ).ToList();

                    foreach ( var descendantId in descendantIdList )
                    {
                        categoryIdList.Add( descendantId );
                    }
                }
            }

            return categoryIdList;
        }

        #region Helper Classes

        /// <summary>
        /// A log device to proces messages for a specific task.
        /// </summary>
        public class TaskLog
        {
            private List<TaskLogMessage> _Messages = new List<TaskLogMessage>();

            public List<TaskLogMessage> Messages
            {
                get
                {
                    return _Messages;
                }
            }

            public TaskLogMessage GetLastMessage( TaskLogMessage.MessageTypeSpecifier? messageType )
            {
                if ( messageType.HasValue )
                {
                    return _Messages.LastOrDefault( x => x.MessageType == messageType );
                }
                else
                {
                    return _Messages.LastOrDefault();
                }
            }

            public TaskLogMessage LogVerbose( string message, string category = null )
            {
                return LogMessage( TaskLogMessage.MessageTypeSpecifier.Verbose, message, category );
            }

            public TaskLogMessage LogInfo( string message, string category = null )
            {
                return LogMessage( TaskLogMessage.MessageTypeSpecifier.Info, message, category );
            }
            public TaskLogMessage LogWarning( string message, string category = null )
            {
                return LogMessage( TaskLogMessage.MessageTypeSpecifier.Warning, message, category );
            }

            public TaskLogMessage LogError( string message, string category = null )
            {
                return LogMessage( TaskLogMessage.MessageTypeSpecifier.Error, message, category );
            }
            public TaskLogMessage LogProgress( string message, string category = null )
            {
                return LogMessage( TaskLogMessage.MessageTypeSpecifier.Progress, message, category );
            }

            public TaskLogMessage LogMessage( TaskLogMessage.MessageTypeSpecifier messageType, string message, string category = null )
            {
                var newMessage = new TaskLogMessage { MessageType = messageType, Message = message, Category = category };

                Trace.WriteLine( newMessage.ToString() );

                _Messages.Add( newMessage );

                return newMessage;
            }

            public class TaskLogMessage
            {
                public enum MessageTypeSpecifier
                {
                    Info = 0,
                    Verbose = 1,
                    Warning = 2,
                    Error = 3,
                    Progress = 4
                }

                public MessageTypeSpecifier MessageType { get; set; } = MessageTypeSpecifier.Info;

                public string Category { get; set; }
                public string Message { get; set; }

                public override string ToString()
                {
                    var msg = $"[{MessageType}] ";

                    if ( string.IsNullOrWhiteSpace( Category ) )
                    {
                        msg += $"{Category}:";
                    }

                    msg += $" {Message}";

                    return msg;
                }
            }
        }

        #endregion
    }
}