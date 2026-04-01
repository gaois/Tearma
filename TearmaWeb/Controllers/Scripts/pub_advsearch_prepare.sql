set nocount on;

-------------------------------------------------------------------------
-- return lingo configuration
-------------------------------------------------------------------------
select *
from configs
where id = 'lingo';

-------------------------------------------------------------------------
-- precompute child counts for all metadata items
-------------------------------------------------------------------------
;with childCounts as (
    select parentID, count(*) as hasChildren
    from metadata
    group by parentID
)

-------------------------------------------------------------------------
-- return POS labels (always hasChildren = 0)
-- and domains (with computed child counts)
-------------------------------------------------------------------------
select 
    id,
    type,
    json,
    sortkeyGA,
    0 as hasChildren
from metadata
where type = 'posLabel'
union all
select 
    m.id,
    m.type,
    m.json,
    m.sortkeyGA,
    isnull(cc.hasChildren, 0) as hasChildren
from metadata as m
left join childCounts as cc on cc.parentID = m.id
where m.type = 'domain'
order by sortkeyGA;
