use [tearma]

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

-------------------------------------------------------------------------
-- compute matching entry IDs
-------------------------------------------------------------------------
declare @matches table(entry_id int, rownum int);

-------------------------------------------------------------------------
-- NON-FTS SEARCH
-------------------------------------------------------------------------
if (@extent <> 'ft')
begin
	insert into @matches(entry_id, rownum)
		/**select**/
		from (
			select distinct e.id, e.sortkeyEN, e.sortkeyGA
			from entries e
			inner join entry_term et on et.entry_id = e.id
			inner join terms t on t.id = et.term_id
			left join term_pos tp on tp.term_id = t.id
			left join entry_domain ed on ed.entry_id = e.id
			/**where1**/
		) as ret;
end

-------------------------------------------------------------------------
-- FTS SEARCH
-------------------------------------------------------------------------
else
begin
    ---------------------------------------------------------------------
    -- tokenise input
    ---------------------------------------------------------------------
    declare @words table(word nvarchar(255));

    insert into @words(word)
		select display_term
		from sys.dm_fts_parser(N'"' + replace(@word, '"', ' ') + '"', 0, null, 1)
		where special_term in ('Noise Word', 'Exact Match');

    ---------------------------------------------------------------------
    -- working sets
    ---------------------------------------------------------------------
    declare @relateds table(entry_id int, lang nvarchar(10), term_id int);
    declare @temp     table(entry_id int, term_id int);
    declare @tokens   table(token nvarchar(255) collate Latin1_General_CI_AS, lang nvarchar(10));

    ---------------------------------------------------------------------
    -- FIRST TOKEN
    ---------------------------------------------------------------------
    declare @word1 nvarchar(255);
    select top (1) @word1 = word from @words;

    if (@word1 is not null and @word1 <> '')
    begin
        delete from @tokens;
        insert into @tokens(token, lang) values(@word1, '');

        insert into @tokens(token, lang)
            select token, lang
            from flex
            where lemma = @word1;
			
		insert into @relateds(entry_id, lang, term_id)
			select distinct e.id, t.lang, t.id
			from entries e
			inner join entry_term et on et.entry_id = e.id
			inner join terms t on t.id = et.term_id
			inner join words w on w.term_id = t.id
			inner join @tokens tok on tok.token = w.word
			/**where2**/;
    end

    ---------------------------------------------------------------------
    -- SECOND TOKEN
    ---------------------------------------------------------------------
    declare @word2 nvarchar(255);
    select top (2) @word2 = word from @words;

    if (@word2 <> @word1)
    begin
        delete from @temp;
        insert into @temp(entry_id, term_id)
            select entry_id, term_id from @relateds;

        delete from @relateds;

        delete from @tokens;
        insert into @tokens(token, lang) values(@word2, '');

        insert into @tokens(token, lang)
            select token, lang
            from flex
            where lemma = @word2;
			
		insert into @relateds(entry_id, lang, term_id)
			select distinct e.id, t.lang, t.id
			from entries e
			inner join @temp temp on temp.entry_id = e.id
			inner join entry_term et on et.entry_id = e.id
			inner join terms t on t.id = et.term_id
			inner join words w on w.term_id = t.id
			inner join @tokens tok on tok.token = w.word
			/**where3**/;
    end

    ---------------------------------------------------------------------
    -- THIRD TOKEN
    ---------------------------------------------------------------------
    declare @word3 nvarchar(255);
    select top (3) @word3 = word from @words;

    if (@word3 <> @word2 and @word3 <> @word1)
    begin
        delete from @temp;
        insert into @temp(entry_id, term_id)
            select entry_id, term_id from @relateds;

        delete from @relateds;

        delete from @tokens;
        insert into @tokens(token, lang) values(@word3, '');

        insert into @tokens(token, lang)
            select token, lang
            from flex
            where lemma = @word3;
			
		insert into @relateds(entry_id, lang, term_id)
			select distinct e.id, t.lang, t.id
			from entries e
			inner join @temp temp on temp.entry_id = e.id
			inner join entry_term et on et.entry_id = e.id
			inner join terms t on t.id = et.term_id
			inner join words w on w.term_id = t.id
			inner join @tokens tok on tok.token = w.word
			/**where3**/;
    end

    ---------------------------------------------------------------------
    -- FINAL FTS MATCH INSERT
    ---------------------------------------------------------------------
	insert into @matches(entry_id, rownum)
		/**select**/
		from (
			select distinct e.id, e.sortkeyEN, e.sortkeyGA
			from entries e
			inner join @relateds ids on ids.entry_id = e.id
			left join term_pos tp on tp.term_id = ids.term_id
			left join entry_domain ed on ed.entry_id = ids.entry_id
			/**where4**/
		) as ret;
end

-------------------------------------------------------------------------
-- PAGING
-------------------------------------------------------------------------
declare @lastRow int;
select @lastRow = count(*) from @matches;

declare @maxPage int;
set @maxPage = @lastRow / 100;
if (@lastRow % 100 > 0) set @maxPage = @maxPage + 1;

if (@page > @maxPage) set @page = @maxPage;

declare @firstRow int;
set @firstRow = (@page * 100) - 99;

-------------------------------------------------------------------------
-- RETURN XREF TARGETS
-------------------------------------------------------------------------
select *
from entries trg
inner join entry_xref x on x.target_entry_id = trg.id
inner join @matches src on src.entry_id = x.source_entry_id;

-------------------------------------------------------------------------
-- RETURN MATCHES
-------------------------------------------------------------------------
select @total = count(*) from @matches;

select top (100) e.*
from entries e
inner join @matches m on m.entry_id = e.id
where m.rownum >= @firstRow
order by m.rownum;

-------------------------------------------------------------------------
-- RETURN PAGER
-------------------------------------------------------------------------
select @page as currentPage, @maxPage as maxPage;
