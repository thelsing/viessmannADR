//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace viessmannAdr
{
    using System;
    using System.Collections.Generic;
    
    public partial class DisplayConditionGroup
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DisplayConditionGroup()
        {
            this.DisplayConditions = new HashSet<DisplayCondition>();
            this.ChildDisplayConditionGroups = new HashSet<DisplayConditionGroup>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public byte Type { get; set; }
        public int ParentId { get; set; }
        public string Description { get; set; }
        public int EventTypeIdDest { get; set; }
        public int EventTypeGroupIdDest { get; set; }
    
        public virtual EventTypeGroup EventTypeGroupDest { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DisplayCondition> DisplayConditions { get; set; }
        public virtual DisplayConditionGroup ParentDisplayConditionGroup { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DisplayConditionGroup> ChildDisplayConditionGroups { get; set; }
        public virtual EventType TargetEventType { get; set; }
    }
}
