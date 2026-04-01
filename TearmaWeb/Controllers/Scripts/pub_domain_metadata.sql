set nocount on;

-------------------------------------------------------------------------
-- lingo
-------------------------------------------------------------------------
select *
from configs
where id = 'lingo';

-------------------------------------------------------------------------
-- metadata (ga or en)
-------------------------------------------------------------------------
if @lang = 'ga'
begin
    select id, type, json, sortkeyGA as sortkey, 0 as hasChildren
    from metadata
    where type in ('acceptLabel', 'inflectLabel', 'posLabel')
    union all
    select m.id, m.type, m.json, m.sortkeyGA as sortkey, count(ch.id) as hasChildren
    from metadata m
    left join metadata ch on ch.parentID = m.id
    where m.type = 'domain'
    group by m.id, m.type, m.json, m.sortkeyGA
    order by sortkey;
end
else
begin
    select id, type, json, sortkeyEN as sortkey, 0 as hasChildren
    from metadata
    where type in ('acceptLabel', 'inflectLabel', 'posLabel')
    union all
    select m.id, m.type, m.json, m.sortkeyEN as sortkey, count(ch.id) as hasChildren
    from metadata m
    left join metadata ch on ch.parentID = m.id
    where m.type = 'domain'
    group by m.id, m.type, m.json, m.sortkeyEN
    order by sortkey;
end
