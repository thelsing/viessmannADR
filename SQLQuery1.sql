select  distinct  t.Address from ecnEventGroupValueCache l join ecnEventType t on l.EventTypeId = t.Id
where t.Address like '%K00%'

select EventTypeGroupIdDest, count(*) from ecnDisplayConditionGroup group by EventTypeGroupIdDest having count(*) > 1

select * from ecnEventValueType where id in (
select EventTypeValueCondition from ecnDisplayCondition where ConditionGroupId in(

select Id from ecnDisplayConditionGroup where EventTypeGroupIdDest = 22691))