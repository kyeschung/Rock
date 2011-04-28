//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the T4\Model.tt template.
//
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
//
// THIS WORK IS LICENSED UNDER A CREATIVE COMMONS ATTRIBUTION-NONCOMMERCIAL-
// SHAREALIKE 3.0 UNPORTED LICENSE:
// http://creativecommons.org/licenses/by-nc-sa/3.0/
//
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

using Rock.Models;

namespace Rock.Models.Cms
{
    [Table( "cmsBlockInstance" )]
    public partial class BlockInstance : ModelWithAttributes, IAuditable
    {
		[DataMember]
		public Guid Guid { get; set; }
		
		[DataMember]
		public bool System { get; set; }
		
		[DataMember]
		public int? PageId { get; set; }
		
		[MaxLength( 100 )]
		[DataMember]
		public string Layout { get; set; }
		
		[DataMember]
		public int BlockId { get; set; }
		
		[MaxLength( 100 )]
		[DataMember]
		public string Zone { get; set; }
		
		[DataMember]
		public int Order { get; set; }
		
		[DataMember]
		public int OutputCacheDuration { get; set; }
		
		[DataMember]
		public DateTime? CreatedDateTime { get; set; }
		
		[DataMember]
		public DateTime? ModifiedDateTime { get; set; }
		
		[DataMember]
		public int? CreatedByPersonId { get; set; }
		
		[DataMember]
		public int? ModifiedByPersonId { get; set; }
		
		[NotMapped]
		public override string AuthEntity { get { return "Cms.BlockInstance"; } }

		public virtual ICollection<HtmlContent> HtmlContents { get; set; }

		public virtual Block Block { get; set; }

		public virtual Page Page { get; set; }

		public virtual Crm.Person CreatedByPerson { get; set; }

		public virtual Crm.Person ModifiedByPerson { get; set; }
    }

    public partial class BlockInstanceConfiguration : EntityTypeConfiguration<BlockInstance>
    {
        public BlockInstanceConfiguration()
        {
			this.HasRequired( p => p.Block ).WithMany( p => p.BlockInstances ).HasForeignKey( p => p.BlockId );
			this.HasOptional( p => p.Page ).WithMany( p => p.BlockInstances ).HasForeignKey( p => p.PageId );
			this.HasOptional( p => p.CreatedByPerson ).WithMany().HasForeignKey( p => p.CreatedByPersonId );
			this.HasOptional( p => p.ModifiedByPerson ).WithMany().HasForeignKey( p => p.ModifiedByPersonId );
		}
    }
}
