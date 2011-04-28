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
    [Table( "cmsBlogTag" )]
    public partial class BlogTag : ModelWithAttributes
    {
		[DataMember]
		public Guid Guid { get; set; }
		
		[DataMember]
		public int BlogId { get; set; }
		
		[MaxLength( 50 )]
		[DataMember]
		public string Name { get; set; }
		
		[NotMapped]
		public override string AuthEntity { get { return "Cms.BlogTag"; } }

		public virtual ICollection<BlogPost> BlogPosts { get; set; }

		public virtual Blog Blog { get; set; }
    }

    public partial class BlogTagConfiguration : EntityTypeConfiguration<BlogTag>
    {
        public BlogTagConfiguration()
        {
			this.HasRequired( p => p.Blog ).WithMany( p => p.BlogTags ).HasForeignKey( p => p.BlogId );
		}
    }
}
