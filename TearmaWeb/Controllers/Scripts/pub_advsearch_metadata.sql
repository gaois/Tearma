set nocount on;

-------------------------------------------------------------------------
-- return lingo configuration
-------------------------------------------------------------------------
select *
from configs
where id = 'lingo';

-------------------------------------------------------------------------
-- metadata - precompute child counts once
-------------------------------------------------------------------------
;with childCounts as (
    select parentID, count(*) as hasChildren
    from metadata
    group by parentID
)
select
    m.id,
    m.type,
    m.json,
    m.sortkeyGA,
    isnull(cc.hasChildren, 0) as hasChildren
from metadata m
left join childCounts cc
    on cc.parentID = m.id
where m.type in ('acceptLabel', 'inflectLabel', 'posLabel', 'domain')
order by m.sortkeyGA;
