SET ANSI_WARNINGS OFF

update entries set
  cStatus=JSON_VALUE(json, '$.cStatus')
, pStatus=JSON_VALUE(json, '$.pStatus')
, dateStamp=JSON_VALUE(json, '$.dateStamp')
, tod=JSON_VALUE(json, '$.tod')

truncate table entry_xref
insert into entry_xref(source_entry_id, target_entry_id)
select e.id as source_id, target_id
from entries as e
cross apply openjson(e.json, '$.xrefs') with(
  target_id int '$'
  ) as pj

truncate table entry_domain
insert into entry_domain(entry_id, superdomain, subdomain)
select e.id, pj.superdomain, pj.subdomain
from entries as e
cross apply openjson(e.json, '$.domains') with(
  superdomain int '$.superdomain'
, subdomain int '$.subdomain'
) as pj

update metadata set
  sortkeyGA=JSON_VALUE(json, '$.title.ga')
, sortkeyEN=JSON_VALUE(json, '$.title.en')

truncate table entry_term
insert into entry_term(entry_id, term_id, accept)
select e.id as entryID, pj.termID, pj.accept
from entries as e
cross apply openjson(e.json, '$.desigs') with(
  termID int '$.term.id'
, accept int '$.accept'
) as pj

truncate table terms
insert into terms(id, lang, wording)
select distinct id, lang, wording from(
	select
	  convert(int, json_value(ext.value, '$.term.id')) as id
	--, json_query(ext.value, '$.term') as json
	, json_value(ext.value, '$.term.lang') as lang
	, json_value(ext.value, '$.term.wording') as wording
	from entries as e
	cross apply openjson(e.json, '$.desigs') as ext
) as t

truncate table words
insert into words(term_id, word)
select t.id, w.display_term
from terms as t
cross apply (
	select display_term
	from sys.dm_fts_parser(N'"'+replace(t.wording, '"', ' ')+'"', 0, null, 1)
	where special_term in ('Noise Word', 'Exact Match')
) as w

truncate table term_pos
insert into term_pos(term_id, pos_id)
select distinct term_id, pos_id from(
	select
	  convert(int, json_value(ext.value, '$.term.id')) as term_id
	, json_value(annot.value, '$.label.value') as pos_id
	from entries as e
	cross apply openjson(e.json, '$.desigs') as ext
	cross apply openjson(ext.value, '$.term.annots') as annot
	where json_value(annot.value, '$.label.type')='posLabel'
) as t

truncate table spelling
insert into spelling(term_id, word, [A],[B],[C],[D],[E],[F],[G],[H],[I],[J],[K],[L],[M],[N],[O],[P],[Q],[R],[S],[T],[U],[V],[W],[X],[Y],[Z], [length])
select t.id, t.wording, [A],[B],[C],[D],[E],[F],[G],[H],[I],[J],[K],[L],[M],[N],[O],[P],[Q],[R],[S],[T],[U],[V],[W],[X],[Y],[Z], LEN(t.wording)
from terms as t
cross apply (
	select [A],[B],[C],[D],[E],[F],[G],[H],[I],[J],[K],[L],[M],[N],[O],[P],[Q],[R],[S],[T],[U],[V],[W],[X],[Y],[Z]
	from dbo.characterize(t.wording)
) as c
where len(t.wording)<=10

update entries set sortkeyGA=null, sortkeyEN=null
declare @temp table(entry_id int, sortkey nvarchar(max), listingOrder int)
insert into @temp(entry_id, sortkey, listingOrder)
	select e.id, JSON_VALUE(pj.value, '$.term.wording'), pj.[key]
	from entries as e
	cross apply openjson(e.json, '$.desigs') as pj
	where JSON_VALUE(pj.value, '$.term.lang')='ga'
	update e set e.sortkeyGA=t.sortkey
		from entries as e
		inner join @temp as t on t.entry_id=e.id
		where e.sortkeyGA is null
delete from @temp
insert into @temp(entry_id, sortkey, listingOrder)
	select e.id, JSON_VALUE(pj.value, '$.term.wording'), pj.[key]
	from entries as e
	cross apply openjson(e.json, '$.desigs') as pj
	where JSON_VALUE(pj.value, '$.term.lang')='en'
	update e set e.sortkeyEN=t.sortkey
		from entries as e
		inner join @temp as t on t.entry_id=e.id
		where e.sortkeyEN is null

SET ANSI_WARNINGS ON
