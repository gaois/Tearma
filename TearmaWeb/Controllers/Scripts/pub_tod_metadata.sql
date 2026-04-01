set nocount on;

-- Lingo
select * 
from configs 
where id = 'lingo';

-- Metadata
select m.*, 0 as hasChildren
from metadata m
where m.type in ('acceptLabel', 'inflectLabel', 'posLabel', 'domain')
order by m.sortkeyGA;
