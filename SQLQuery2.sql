select eg.Address, e.Address, dg.Id, v.EnumAddressValue,c.Condition
from ecnEventTypeGroup eg
join ecnDisplayConditionGroup dg on eg.Id = dg.EventTypeGroupIdDest
join ecnDisplayCondition c on c.ConditionGroupId = dg.Id
join ecnEventType e on c.EventTypeIdCondition = e.Id
left outer join ecnEventValueType v on c.EventTypeValueCondition = v.id
where DataPointTypeId = 350
order by 1

select ConditionGroupId, EventTypeIdCondition,count(*) from (
select ConditionGroupId, EventTypeIdCondition, Condition 
from ecnDisplayCondition 
where Condition in (0,1)
group by ConditionGroupId, EventTypeIdCondition, Condition
) a group by a.ConditionGroupId, EventTypeIdCondition having count(*) > 1



select et.Address, dg.Id, e.Address, c.Condition, c.ConditionValue, v.EnumAddressValue,v.* from  ecnDataPointTypeEventTypeLink del
join ecnEventType et on del.EventTypeId = et.Id
join ecnDisplayConditionGroup dg on et.Id = dg.EventTypeIdDest
join ecnDisplayCondition c on c.ConditionGroupId = dg.Id
join ecnEventType e on c.EventTypeIdCondition = e.Id
left outer join ecnEventValueType v on c.EventTypeValueCondition = v.id
where DataPointTypeId = 350
order by 1




select * from ecnDisplayConditionGroup where ParentId <> -1