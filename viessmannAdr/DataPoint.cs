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
    
    public partial class DataPoint
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DataPoint()
        {
            this.ecnEventGroupValueCache = new HashSet<EventGroupValue>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public int DataPointTypeId { get; set; }
        public int DeviceId { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public string InformationDataSetXML { get; set; }
        public int StatusEventTypeId { get; set; }
        public bool Deleted { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<EventGroupValue> ecnEventGroupValueCache { get; set; }
        public virtual DatapointType DatapointType { get; set; }
    }
}
