set nocount on;

-- Lingo
select * 
from configs 
where id = 'lingo';

-- Metadata
select *, 0 as hasChildren
from metadata
where type in ('acceptLabel', 'inflectLabel', 'posLabel', 'domain')
order by sortkeyGA;
