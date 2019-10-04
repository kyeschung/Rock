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
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// Represents a Streak Type Achievement Type in Rock.
    /// </summary>
    [RockDomain( "Streaks" )]
    [Table( "StreakTypeAchievementType" )]
    [DataContract]
    public partial class StreakTypeAchievementType : Model<StreakTypeAchievementType>, IHasActiveFlag
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the Id of the <see cref="Model.StreakType"/> to which this StreakTypeAchievementType belongs. This property is required.
        /// </summary>
        [Required]
        [DataMember( IsRequired = true )]
        public int StreakTypeId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the achievement component <see cref="EntityType"/>
        /// </summary>
        [Required]
        [DataMember( IsRequired = true )]
        public int AchievementEntityTypeId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="WorkflowType"/> to be triggered when an achievement is started
        /// </summary>
        [DataMember]
        public int? AchievementStartWorkflowTypeId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="WorkflowType"/> to be triggered when an achievement is ended
        /// </summary>
        [DataMember]
        public int? AchievementEndWorkflowTypeId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="StepType"/> of which a <see cref="Step"/> will be created when an achievement is completed
        /// </summary>
        [DataMember]
        public int? AchievementStepTypeId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="StepStatus"/> of which a <see cref="Step"/> will be created when an achievement is completed
        /// </summary>
        [DataMember]
        public int? AchievementStepStatusId { get; set; }

        /// <summary>
        /// Gets or sets the lava template used to render a badge.
        /// </summary>
        [DataMember]
        public string BadgeLavaTemplate { get; set; }

        /// <summary>
        /// Gets or sets the lava template used to render results.
        /// </summary>
        [DataMember]
        public string ResultsLavaTemplate { get; set; }

        /// <summary>
        /// Gets or sets the icon CSS class.
        /// </summary>
        [MaxLength( 100 )]
        [DataMember]
        public string AchievementIconCssClass { get; set; }

        /// <summary>
        /// Gets or sets the maximum accomplishments allowed.
        /// </summary>
        /// <value>
        /// The maximum accomplishments allowed.
        /// </value>
        [Range( 1, int.MaxValue )]
        [DataMember]
        public int? MaxAccomplishmentsAllowed { get; set; } = 1;

        /// <summary>
        /// Gets or sets whether over achievement is allowed. This cannot be true if <see cref="MaxAccomplishmentsAllowed"/> is greater than 1.
        /// </summary>
        /// <value>
        /// The allow over achievement.
        /// </value>
        [DataMember]
        public bool AllowOverAchievement { get; set; }

        /// <summary>
        /// Gets or sets the category identifier.
        /// </summary>
        /// <value>
        /// The category identifier.
        /// </value>
        [DataMember]
        public int? CategoryId { get; set; }

        #endregion Entity Properties

        #region IHasActiveFlag

        /// <summary>
        /// Gets a value indicating whether this instance is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is active; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsActive { get; set; }

        #endregion IHasActiveFlag

        #region Virtual Properties

        /// <summary>
        /// Gets or sets the <see cref="Model.StreakType"/>.
        /// </summary>
        [DataMember]
        public virtual StreakType StreakType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EntityType"/> of the component.
        /// </summary>
        [DataMember]
        public virtual EntityType AchievementEntityType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="WorkflowType"/> to be launched when the achievement starts.
        /// </summary>
        [DataMember]
        public virtual WorkflowType AchievementStartWorkflowType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="WorkflowType"/> to be launched when the achievement ends.
        /// </summary>
        [DataMember]
        public virtual WorkflowType AchievementEndWorkflowType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="StepType"/> to be created when the achievement is completed.
        /// </summary>
        [DataMember]
        public virtual StepType AchievementStepType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="StepStatus"/> to be used for the <see cref="StepType"/> created when the achievement is completed.
        /// </summary>
        [DataMember]
        public virtual StepStatus AchievementStepStatus { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Model.Category"/>.
        /// </summary>
        [DataMember]
        public virtual Category Category { get; set; }

        /// <summary>
        /// Gets or sets the streak achievement attempts.
        /// </summary>
        /// <value>
        /// The streak type achievement types.
        /// </value>
        [DataMember]
        public virtual ICollection<StreakAchievementAttempt> StreakAchievementAttempts
        {
            get => _streakAchievementAttempts ?? ( _streakAchievementAttempts = new Collection<StreakAchievementAttempt>() );
            set => _streakAchievementAttempts = value;
        }
        private ICollection<StreakAchievementAttempt> _streakAchievementAttempts;

        #endregion Virtual Properties

        #region Entity Configuration

        /// <summary>
        /// Streak Type Achievement Type Configuration class.
        /// </summary>
        public partial class StreakTypeAchievementTypeConfiguration : EntityTypeConfiguration<StreakTypeAchievementType>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="StreakTypeAchievementTypeConfiguration"/> class.
            /// </summary>
            public StreakTypeAchievementTypeConfiguration()
            {
                HasRequired( stat => stat.StreakType ).WithMany( st => st.StreakTypeAchievementTypes ).HasForeignKey( stat => stat.StreakTypeId ).WillCascadeOnDelete( true );
                HasRequired( stat => stat.AchievementEntityType ).WithMany().HasForeignKey( stat => stat.AchievementEntityTypeId ).WillCascadeOnDelete( true );
                HasRequired( stat => stat.AchievementStartWorkflowType ).WithMany().HasForeignKey( stat => stat.AchievementStartWorkflowTypeId ).WillCascadeOnDelete( false );
                HasRequired( stat => stat.AchievementEndWorkflowType ).WithMany().HasForeignKey( stat => stat.AchievementEndWorkflowTypeId ).WillCascadeOnDelete( false );
                HasRequired( stat => stat.AchievementStepType ).WithMany( st => st.StreakTypeAchievementTypes ).HasForeignKey( stat => stat.AchievementStepTypeId ).WillCascadeOnDelete( false );
                HasRequired( stat => stat.AchievementStepStatus ).WithMany().HasForeignKey( stat => stat.AchievementStepStatusId ).WillCascadeOnDelete( false );

                HasOptional( stat => stat.Category ).WithMany().HasForeignKey( stat => stat.CategoryId ).WillCascadeOnDelete( false );
            }
        }

        #endregion Entity Configuration

        #region Overrides

        /// <summary>
        /// Gets a value indicating whether this instance is valid.
        /// </summary>
        public override bool IsValid
        {
            get
            {
                var isValid = base.IsValid;

                if ( MaxAccomplishmentsAllowed > 1 && AllowOverAchievement )
                {
                    ValidationResults.Add( new ValidationResult( "MaxAccomplishmentsAllowed cannot be greater than 1 if AllowOverAchievement is set" ) );
                    isValid = false;
                }

                return isValid;
            }
        }

        #endregion Overrides
    }
}