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
    
    public partial class EventType
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public EventType()
        {
            this.DatapointTypes = new HashSet<DatapointType>();
            this.EventValueTypes = new HashSet<EventValueType>();
            this.EventTypeGroupLinks = new HashSet<EventTypeEventTypeGroupLink>();
            this.DisplayConditions = new HashSet<DisplayCondition>();
            this.DisplayConditionGroups = new HashSet<DisplayConditionGroup>();
        }
    
        public int Id { get; set; }
        public bool EnumType { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Conversion { get; set; }
        public string Description { get; set; }
        public int Priority { get; set; }
        public bool Filtercriterion { get; set; }
        public bool Reportingcriterion { get; set; }
        public int Type { get; set; }
        public string URL { get; set; }
        public string DefaultValue { get; set; }
        public int ObjectId { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DatapointType> DatapointTypes { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<EventValueType> EventValueTypes { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<EventTypeEventTypeGroupLink> EventTypeGroupLinks { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DisplayCondition> DisplayConditions { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DisplayConditionGroup> DisplayConditionGroups { get; set; }
    }
}
