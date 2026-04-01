USE [tearma]
GO
/****** Object:  UserDefinedFunction [dbo].[characterize]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create function [dbo].[characterize](@text nvarchar(255))
returns @chars table(A int, B int, C int, D int, E int,
                     F int, G int, H int, I int, J int,
                     K int, L int, M int, N int, O int,
                     P int, Q int, R int, S int, T int,
                     U int, V int, W int, X int, Y int,
                     Z int)
begin
    declare @temp table(Base nvarchar(1), [Count] int)
    insert into @temp(Base, [Count])
    select c.base, COUNT(t.Result) as [Count]
    from chars as c
    left outer join dbo.substrings(dbo.spartanize(@text), 1) as t on c.variant=t.Result
    group by c.Base

    insert into @chars
    select [A],[B],[C],[D],[E],[F],[G],[H],[I],[J],[K],[L],[M],[N],[O],[P],[Q],[R],[S],[T],[U],[V],[W],[X],[Y],[Z]
    from (select [Base], [Count] from @temp) as src
    pivot(sum([Count]) for Base in([A],[B],[C],[D],[E],[F],[G],[H],[I],[J],[K],[L],[M],[N],[O],[P],[Q],[R],[S],[T],[U],[V],[W],[X],[Y],[Z])) as piv

    return
end
GO
/****** Object:  UserDefinedFunction [dbo].[expandDomainID]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE function [dbo].[expandDomainID]
(   
  @domainID int
, @level int = 0
)
returns @ret table(domainID int) 
as
begin
    insert into @ret(domainID) values(@domainID)
    if @level<10
    begin
        declare @subdomainID int
        DECLARE crsr CURSOR FAST_FORWARD FOR
            select m.id
            from metadata as m
            where m.type='domain' and m.parentID=@domainID
        OPEN crsr
        FETCH NEXT FROM crsr INTO @subdomainID
        WHILE @@FETCH_STATUS = 0  
        BEGIN  
            insert into @ret(domainID) select domainID from dbo.expandDomainID(@subdomainID, @level+1)
            FETCH NEXT FROM crsr INTO @subdomainID
        END 
        CLOSE crsr
        DEALLOCATE crsr
    end
    return 
end
GO
/****** Object:  UserDefinedFunction [dbo].[levenshtein]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

create function [dbo].[levenshtein]( @s varchar(50), @t varchar(50) ) 
--Returns the Levenshtein Distance between strings s1 and s2.
--Original developer: Michael Gilleland    http://www.merriampark.com/ld.htm
--Translated to TSQL by Joseph Gama
returns varchar(50)
as
BEGIN
DECLARE @d varchar(2500), @LD int, @m int, @n int, @i int, @j int,
@s_i char(1), @t_j char(1),@cost int
--Step 1
SET @n=LEN(@s)
SET @m=LEN(@t)
SET @d=replicate(CHAR(0),2500)
If @n = 0
    BEGIN
    SET @LD = @m
    GOTO done
    END
If @m = 0
    BEGIN
    SET @LD = @n
    GOTO done
    END
--Step 2
SET @i=0
WHILE @i<=@n
    BEGIN
    SET @d=STUFF(@d,@i+1,1,CHAR(@i))--d(i, 0) = i
    SET @i=@i+1
    END

SET @i=0
WHILE @i<=@m
    BEGIN
    SET @d=STUFF(@d,@i*(@n+1)+1,1,CHAR(@i))--d(0, j) = j
    SET @i=@i+1
    END
--goto done
--Step 3
    SET @i=1
    WHILE @i<=@n
        BEGIN
        SET @s_i=(substring(@s,@i,1))
--Step 4
    SET @j=1
    WHILE @j<=@m
        BEGIN
        SET @t_j=(substring(@t,@j,1))
        --Step 5
        If @s_i = @t_j
            SET @cost=0
        ELSE
            SET @cost=1
--Step 6
        SET @d=STUFF(@d,@j*(@n+1)+@i+1,1,CHAR(dbo.MIN3(
        ASCII(substring(@d,@j*(@n+1)+@i-1+1,1))+1,
        ASCII(substring(@d,(@j-1)*(@n+1)+@i+1,1))+1,
        ASCII(substring(@d,(@j-1)*(@n+1)+@i-1+1,1))+@cost)
        ))
        SET @j=@j+1
        END
    SET @i=@i+1
    END      
--Step 7
SET @LD = ASCII(substring(@d,@n*(@m+1)+@m+1,1))
done:
--RETURN @LD
--I kept this code that can be used to display the matrix with all calculated values
--From Query Analyser it provides a nice way to check the algorithm in action
--
RETURN @LD
--declare @z varchar(8000)
--set @z=''
--SET @i=0
--WHILE @i<=@n
--	BEGIN
--	SET @j=0
--	WHILE @j<=@m
--		BEGIN
--		set @z=@z+CONVERT(char(3),ASCII(substring(@d,@i*(@m+1 )+@j+1 ,1)))
--		SET @j=@j+1 
--		END
--	SET @i=@i+1
--	END
--print dbo.wrap(@z,3*(@n+1))
END
GO
/****** Object:  UserDefinedFunction [dbo].[min3]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create FUNCTION [dbo].[min3](@a int, @b int,  @c int )
RETURNS int
AS
BEGIN
    DECLARE @m INT
    SET @m = @a

    IF @b < @m SET @m = @b
    IF @c < @m SET @m = @c
    
    RETURN @m
END
GO
/****** Object:  UserDefinedFunction [dbo].[substrings]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

create function [dbo].[substrings]
(   
    @str nvarchar(max),
    @length int
)
returns @Results table( Result nvarchar(50) collate Latin1_General_BIN2, Position int ) 
AS
begin
    declare @pos int
    set @pos=0
    declare @s nvarchar(50)
    while len(@str) > 0
    begin
        set @s = left(@str, @length)
        set @str = right(@str, len(@str) - @length)
        insert @Results(Result, Position) values (@s, @pos)
        set @pos=@pos+1
    end
    return 
end
GO
/****** Object:  UserDefinedFunction [dbo].[substrings_inline]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[substrings_inline]
(
    @str nvarchar(255),
    @length int
)
RETURNS TABLE
AS
RETURN
(
    WITH n AS (
        SELECT TOP (LEN(@str))
            ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS pos
        FROM sys.all_objects
    )
    SELECT
        Result   = SUBSTRING(@str, pos + 1, @length) COLLATE Latin1_General_BIN2,
        Position = pos
    FROM n
    WHERE pos + @length <= LEN(@str)
);

GO
/****** Object:  UserDefinedFunction [dbo].[spartanize]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE function [dbo].[spartanize](@text nvarchar(255))
returns nvarchar(255)
with schemabinding
begin
    set @text=LOWER(@text);

    set @text=TRIM(REPLACE(@text+' ', 'ize ', 'ise '))
    set @text=TRIM(REPLACE(@text+' ', 'izing ', 'ising '))
    set @text=TRIM(REPLACE(@text+' ', 'izes ', 'ises '))
    set @text=TRIM(REPLACE(@text+' ', 'ized ', 'ised '))
    set @text=TRIM(REPLACE(@text+' ', 'izer ', 'iser '))
    set @text=TRIM(REPLACE(@text+' ', 'ization ', 'isation '))

    set @text=REPLACE(@text, NCHAR(0x005F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x203F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2040), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2054), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE33), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE34), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE4D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE4E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE4F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF3F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x002D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x058A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x05BE), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1400), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1806), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2010), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2011), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2012), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2013), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2014), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2015), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E17), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E1A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E3A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E3B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x301C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x3030), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x30A0), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE31), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE32), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE58), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE63), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF0D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0029), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x005D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x007D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F3B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F3D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x169C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2046), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x207E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x208E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x232A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2769), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x276B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x276D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x276F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2771), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2773), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2775), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x27C6), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x27E7), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x27E9), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x27EB), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x27ED), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x27EF), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2984), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2986), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2988), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x298A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x298C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x298E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2990), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2992), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2994), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2996), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2998), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x29D9), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x29DB), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x29FD), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E23), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E25), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E27), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E29), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x3009), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x300B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x300D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x300F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x3011), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x3015), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x3017), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x3019), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x301B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x301E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x301F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFD3F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE18), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE36), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE38), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE3A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE3C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE3E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE40), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE42), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE44), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE48), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE5A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE5C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE5E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF09), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF3D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF5D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF60), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF63), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x00BB), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2019), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x201D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x203A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E03), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E05), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E0A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E0D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E1D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E21), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x00AB), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2018), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x201B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x201C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x201F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2039), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E02), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E04), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E09), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E0C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E1C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E20), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0021), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0022), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0023), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0025), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0026), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0027), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x002A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x002C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x002E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x002F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x003A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x003B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x003F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0040), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x005C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x00A1), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x00A7), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x00B6), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x00B7), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x00BF), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x037E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0387), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x055A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x055B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x055C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x055D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x055E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x055F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0589), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x05C0), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x05C3), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x05C6), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x05F3), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x05F4), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0609), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x060A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x060C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x060D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x061B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x061E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x061F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x066A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x066B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x066C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x066D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x06D4), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0700), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0701), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0702), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0703), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0704), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0705), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0706), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0707), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0708), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0709), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x070A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x070B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x070C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x070D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x07F7), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x07F8), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x07F9), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0830), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0831), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0832), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0833), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0834), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0835), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0836), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0837), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0838), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0839), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x083A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x083B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x083C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x083D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x083E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x085E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0964), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0965), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0970), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0AF0), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0DF4), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0E4F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0E5A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0E5B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F04), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F05), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F06), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F07), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F08), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F09), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F0A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F0B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F0C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F0D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F0E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F0F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F10), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F11), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F12), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F14), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F85), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0FD0), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0FD1), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0FD2), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0FD3), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0FD4), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0FD9), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0FDA), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x104A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x104B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x104C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x104D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x104E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x104F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x10FB), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1360), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1361), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1362), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1363), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1364), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1365), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1366), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1367), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1368), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x166D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x166E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x16EB), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x16EC), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x16ED), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1735), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1736), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x17D4), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x17D5), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x17D6), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x17D8), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x17D9), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x17DA), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1800), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1801), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1802), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1803), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1804), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1805), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1807), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1808), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1809), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x180A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1944), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1945), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1A1E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1A1F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1AA0), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1AA1), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1AA2), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1AA3), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1AA4), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1AA5), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1AA6), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1AA8), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1AA9), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1AAA), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1AAB), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1AAC), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1AAD), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1B5A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1B5B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1B5C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1B5D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1B5E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1B5F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1B60), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1BFC), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1BFD), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1BFE), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1BFF), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1C3B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1C3C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1C3D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1C3E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1C3F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1C7E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1C7F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1CC0), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1CC1), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1CC2), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1CC3), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1CC4), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1CC5), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1CC6), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1CC7), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1CD3), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2016), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2017), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2020), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2021), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2022), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2023), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2024), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2025), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2026), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2027), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2030), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2031), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2032), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2033), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2034), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2035), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2036), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2037), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2038), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x203B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x203C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x203D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x203E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2041), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2042), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2043), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2047), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2048), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2049), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x204A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x204B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x204C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x204D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x204E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x204F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2050), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2051), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2053), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2055), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2056), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2057), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2058), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2059), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x205A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x205B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x205C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x205D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x205E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2CF9), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2CFA), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2CFB), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2CFC), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2CFE), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2CFF), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2D70), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E00), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E01), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E06), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E07), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E08), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E0B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E0E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E0F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E10), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E11), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E12), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E13), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E14), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E15), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E16), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E18), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E19), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E1B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E1E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E1F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E2A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E2B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E2C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E2D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E2E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E30), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E31), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E32), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E33), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E34), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E35), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E36), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E37), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E38), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E39), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x3001), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x3002), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x3003), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x303D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x30FB), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA4FE), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA4FF), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA60D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA60E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA60F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA673), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA67E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA6F2), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA6F3), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA6F4), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA6F5), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA6F6), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA6F7), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA874), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA875), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA876), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA877), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA8CE), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA8CF), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA8F8), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA8F9), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA8FA), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA92E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA92F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA95F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA9C1), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA9C2), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA9C3), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA9C4), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA9C5), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA9C6), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA9C7), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA9C8), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA9C9), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA9CA), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA9CB), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA9CC), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA9CD), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA9DE), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xA9DF), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xAA5C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xAA5D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xAA5E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xAA5F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xAADE), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xAADF), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xAAF0), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xAAF1), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xABEB), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE10), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE11), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE12), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE13), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE14), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE15), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE16), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE19), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE30), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE45), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE46), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE49), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE4A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE4B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE4C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE50), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE51), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE52), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE54), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE55), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE56), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE57), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE5F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE60), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE61), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE68), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE6A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE6B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF01), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF02), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF03), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF05), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF06), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF07), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF0A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF0C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF0E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF0F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF1A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF1B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF1F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF20), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF3C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF61), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF64), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF65), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0028), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x005B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x007B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F3A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0F3C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x169B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x201A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x201E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2045), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x207D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x208D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2329), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2768), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x276A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x276C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x276E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2770), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2772), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2774), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x27C5), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x27E6), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x27E8), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x27EA), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x27EC), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x27EE), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2983), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2985), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2987), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2989), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x298B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x298D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x298F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2991), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2993), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2995), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2997), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x29D8), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x29DA), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x29FC), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E22), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E24), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E26), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2E28), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x3008), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x300A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x300C), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x300E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x3010), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x3014), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x3016), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x3018), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x301A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x301D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFD3E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE17), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE35), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE37), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE39), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE3B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE3D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE3F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE41), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE43), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE47), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE59), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE5B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFE5D), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF08), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF3B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF5B), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF5F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0xFF62), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x0020), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x00A0), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x1680), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x180E), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2000), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2001), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2002), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2003), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2004), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2005), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2006), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2007), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2008), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2009), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x200A), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x202F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x205F), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x3000), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2029), '' collate Latin1_General_BIN2)
    set @text=REPLACE(@text, NCHAR(0x2028), '' collate Latin1_General_BIN2)
    
    return @text
end
GO
/****** Object:  Table [dbo].[chars]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[chars](
    [base] [nvarchar](1) COLLATE Latin1_General_BIN2 NOT NULL,
    [variant] [nvarchar](1) COLLATE Latin1_General_BIN2 NOT NULL
) ON [PRIMARY]
GO
/****** Object:  UserDefinedFunction [dbo].[characterize_inline]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[characterize_inline]
(
    @text nvarchar(255)
)
RETURNS TABLE
AS
RETURN
(
    WITH base AS (
        SELECT
            UPPER(c.base) AS Base,
            COUNT(t.Result) AS Cnt
        FROM chars AS c
        LEFT JOIN dbo.substrings_inline(dbo.spartanize(@text), 1) AS t
            ON c.variant = t.Result
        GROUP BY UPPER(c.base)
    )
    SELECT
        SUM(CASE WHEN Base = 'A' THEN Cnt ELSE 0 END) AS A,
        SUM(CASE WHEN Base = 'B' THEN Cnt ELSE 0 END) AS B,
        SUM(CASE WHEN Base = 'C' THEN Cnt ELSE 0 END) AS C,
        SUM(CASE WHEN Base = 'D' THEN Cnt ELSE 0 END) AS D,
        SUM(CASE WHEN Base = 'E' THEN Cnt ELSE 0 END) AS E,
        SUM(CASE WHEN Base = 'F' THEN Cnt ELSE 0 END) AS F,
        SUM(CASE WHEN Base = 'G' THEN Cnt ELSE 0 END) AS G,
        SUM(CASE WHEN Base = 'H' THEN Cnt ELSE 0 END) AS H,
        SUM(CASE WHEN Base = 'I' THEN Cnt ELSE 0 END) AS I,
        SUM(CASE WHEN Base = 'J' THEN Cnt ELSE 0 END) AS J,
        SUM(CASE WHEN Base = 'K' THEN Cnt ELSE 0 END) AS K,
        SUM(CASE WHEN Base = 'L' THEN Cnt ELSE 0 END) AS L,
        SUM(CASE WHEN Base = 'M' THEN Cnt ELSE 0 END) AS M,
        SUM(CASE WHEN Base = 'N' THEN Cnt ELSE 0 END) AS N,
        SUM(CASE WHEN Base = 'O' THEN Cnt ELSE 0 END) AS O,
        SUM(CASE WHEN Base = 'P' THEN Cnt ELSE 0 END) AS P,
        SUM(CASE WHEN Base = 'Q' THEN Cnt ELSE 0 END) AS Q,
        SUM(CASE WHEN Base = 'R' THEN Cnt ELSE 0 END) AS R,
        SUM(CASE WHEN Base = 'S' THEN Cnt ELSE 0 END) AS S,
        SUM(CASE WHEN Base = 'T' THEN Cnt ELSE 0 END) AS T,
        SUM(CASE WHEN Base = 'U' THEN Cnt ELSE 0 END) AS U,
        SUM(CASE WHEN Base = 'V' THEN Cnt ELSE 0 END) AS V,
        SUM(CASE WHEN Base = 'W' THEN Cnt ELSE 0 END) AS W,
        SUM(CASE WHEN Base = 'X' THEN Cnt ELSE 0 END) AS X,
        SUM(CASE WHEN Base = 'Y' THEN Cnt ELSE 0 END) AS Y,
        SUM(CASE WHEN Base = 'Z' THEN Cnt ELSE 0 END) AS Z
    FROM base
);

GO
/****** Object:  Table [dbo].[metadata]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[metadata](
    [id] [int] NOT NULL,
    [type] [varchar](255) COLLATE Latin1_General_CI_AI NULL,
    [json] [nvarchar](3000) COLLATE Latin1_General_CI_AI NULL,
    [sortkeyGA] [nvarchar](512) COLLATE Latin1_General_CI_AI NULL,
    [sortkeyEN] [nvarchar](512) COLLATE Latin1_General_CI_AI NULL,
    [parentID] [int] NULL,
 CONSTRAINT [PK__metadata__3213E83FD56CDBC2] PRIMARY KEY CLUSTERED 
(
    [id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  UserDefinedFunction [dbo].[expandDomainID_inline]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

create   function [dbo].[expandDomainID_inline]
(
      @domainID int
    , @level    int = 0
)
returns table
as
return
(
    with cte as (
        -- anchor: include the starting node, regardless of type
        select
              id    = @domainID
            , level = @level

        union all

        -- recursive: walk domain children while level < 10
        select
              m.id
            , c.level + 1
        from metadata m
        join cte c
            on m.parentID = c.id
        where m.type = 'domain'
          and c.level < 10
    )
    select
        id as domainID
    from cte
);
GO
/****** Object:  Table [dbo].[terms]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[terms](
    [id] [int] NOT NULL,
    [json] [nvarchar](max) COLLATE Latin1_General_CI_AI NULL,
    [lang] [nvarchar](10) COLLATE Latin1_General_CI_AI NULL,
    [wording] [nvarchar](255) COLLATE Latin1_General_CI_AS NULL,
    [wordingSpartanized]  AS (([dbo].[spartanize]([wording])) collate Latin1_General_CI_AS) PERSISTED,
    [wording_rev]  AS (reverse([wording])) PERSISTED
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[aux]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[aux](
    [coll] [nchar](10) COLLATE Latin1_General_CI_AI NOT NULL,
    [en] [nvarchar](max) COLLATE Latin1_General_CI_AI NOT NULL,
    [ga] [nvarchar](max) COLLATE Latin1_General_CI_AI NOT NULL,
    [id] [int] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK_aux] PRIMARY KEY CLUSTERED 
(
    [id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[configs]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[configs](
    [id] [nvarchar](255) COLLATE Latin1_General_CI_AI NOT NULL,
    [json] [nvarchar](max) COLLATE Latin1_General_CI_AI NULL,
PRIMARY KEY CLUSTERED 
(
    [id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[entries]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[entries](
    [id] [int] NOT NULL,
    [json] [nvarchar](max) COLLATE Latin1_General_CI_AI NULL,
    [cStatus] [int] NULL,
    [pStatus] [int] NULL,
    [dateStamp] [date] NULL,
    [tod] [date] NULL,
    [sortkeyGA] [nvarchar](255) COLLATE Latin1_General_CI_AS NULL,
    [sortkeyEN] [nvarchar](255) COLLATE Latin1_General_CI_AS NULL,
 CONSTRAINT [PK__entries__3213E83FB84A5A39] PRIMARY KEY CLUSTERED 
(
    [id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[entry_domain]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[entry_domain](
    [entry_id] [int] NULL,
    [superdomain] [int] NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[entry_term]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[entry_term](
    [entry_id] [int] NULL,
    [term_id] [int] NULL,
    [accept] [int] NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[entry_xref]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[entry_xref](
    [source_entry_id] [int] NOT NULL,
    [target_entry_id] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[flex]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[flex](
    [term_id] [int] NULL,
    [lang] [nvarchar](10) COLLATE Latin1_General_CI_AI NOT NULL,
    [lemma] [nvarchar](255) COLLATE Latin1_General_CI_AS NOT NULL,
    [token] [nvarchar](255) COLLATE Latin1_General_CI_AS NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[search_text]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[search_text](
    [id] [int] IDENTITY(1,1) NOT NULL,
    [searchText] [nvarchar](255) COLLATE Latin1_General_CI_AI NOT NULL,
    [created] [datetime] NOT NULL,
 CONSTRAINT [PK_search_text] PRIMARY KEY CLUSTERED 
(
    [id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[similar]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[similar](
    [searchTextID] [int] NOT NULL,
    [similar] [nvarchar](255) COLLATE Latin1_General_CI_AI NOT NULL,
    [diff] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[spelling]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[spelling](
    [term_id] [int] NOT NULL,
    [word] [nvarchar](255) COLLATE Latin1_General_CI_AS NOT NULL,
    [A] [int] NOT NULL,
    [B] [int] NOT NULL,
    [C] [int] NOT NULL,
    [D] [int] NOT NULL,
    [E] [int] NOT NULL,
    [F] [int] NOT NULL,
    [G] [int] NOT NULL,
    [H] [int] NOT NULL,
    [I] [int] NOT NULL,
    [J] [int] NOT NULL,
    [K] [int] NOT NULL,
    [L] [int] NOT NULL,
    [M] [int] NOT NULL,
    [N] [int] NOT NULL,
    [O] [int] NOT NULL,
    [P] [int] NOT NULL,
    [Q] [int] NOT NULL,
    [R] [int] NOT NULL,
    [S] [int] NOT NULL,
    [T] [int] NOT NULL,
    [U] [int] NOT NULL,
    [V] [int] NOT NULL,
    [W] [int] NOT NULL,
    [X] [int] NOT NULL,
    [Y] [int] NOT NULL,
    [Z] [int] NOT NULL,
    [length] [int] NOT NULL,
    [first_char]  AS (left([word],(1))) PERSISTED
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[term_pos]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[term_pos](
    [term_id] [int] NOT NULL,
    [pos_id] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[words]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[words](
    [term_id] [int] NULL,
    [word] [nvarchar](255) COLLATE Latin1_General_CI_AS NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[search_text] ADD  CONSTRAINT [DF_search_text_created]  DEFAULT (getdate()) FOR [created]
GO
ALTER TABLE [dbo].[similar]  WITH CHECK ADD  CONSTRAINT [FK_similar_search_text] FOREIGN KEY([searchTextID])
REFERENCES [dbo].[search_text] ([id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[similar] CHECK CONSTRAINT [FK_similar_search_text]
GO
/****** Object:  StoredProcedure [dbo].[propag_deleteEntry]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[propag_deleteEntry]
  @entryID int
, @collection nvarchar(10) = ''
as
begin

delete from entries where id=@entryID
delete from entry_domain where entry_id=@entryID
delete from entry_xref where source_entry_id=@entryID
delete from entry_term where entry_id=@entryID

--delete orphaned terms:
declare @orphs table(id int)
insert into @orphs(id)
    select t.id
    from terms as t
    left outer join entry_term as et on et.term_id=t.id
    where et.entry_id is null
delete from terms where id in (select id from @orphs)
delete from words where term_id in (select id from @orphs)
delete from spelling where term_id in (select id from @orphs)

end
GO
/****** Object:  StoredProcedure [dbo].[propag_deleteMetadatum]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[propag_deleteMetadatum]
  @id int
, @collection nvarchar(10) = ''
as
begin

delete from metadata where id=@id

end
GO
/****** Object:  StoredProcedure [dbo].[propag_saveConfig]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[propag_saveConfig]
  @id varchar(255)
, @json nvarchar(max)
, @collection nvarchar(10) = ''
as
begin

if (select count(*) from configs where id=@id) = 0
begin
    insert into configs(id, [json]) values(@id, @json)
end
else
begin
    update configs set [json]=@json where id=@id
end

end
GO
/****** Object:  StoredProcedure [dbo].[propag_saveEntry]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE procedure [dbo].[propag_saveEntry]
  @entryID int
, @json nvarchar(max)
, @collection nvarchar(10) = ''
as
begin

if(JSON_VALUE(@json, '$.pStatus')='0')
    exec propag_deleteEntry @entryID=@entryID
else
begin

    if (select count(*) from entries where id=@entryID) = 0 insert into entries(id, [json]) values(@entryID, @json)
    else update entries set [json]=@json where id=@entryID

    update entries set
      cStatus=JSON_VALUE(json, '$.cStatus')
    , pStatus=JSON_VALUE(json, '$.pStatus')
    , dateStamp=JSON_VALUE(json, '$.dateStamp')
    , tod=JSON_VALUE(json, '$.tod')
    where id=@entryID

    delete from entry_xref where source_entry_id=@entryID
    insert into entry_xref(source_entry_id, target_entry_id)
    select e.id as source_id, target_id
    from entries as e
    cross apply openjson(e.json, '$.xrefs') with(
      target_id int '$'
      ) as pj
    where e.id=@entryID

    delete from entry_domain where entry_id=@entryID
    insert into entry_domain(entry_id, superdomain)
    select e.id, pj.superdomain
    from entries as e
    cross apply openjson(e.json, '$.domains') with(
      superdomain int '$'
    ) as pj
    where e.id=@entryID

    delete from entry_term where entry_id=@entryID
    insert into entry_term(entry_id, term_id, accept)
    select e.id as entryID, pj.termID, pj.accept
    from entries as e
    cross apply openjson(e.json, '$.desigs') with(
      termID int '$.term.id'
    , accept int '$.accept'
    ) as pj
    where e.id=@entryID

    declare @termIDs table(id int)
    insert into @termIDs select term_id from entry_term where entry_id=@entryID

    delete from terms where id in (select id from @termIDs)
    insert into terms(id, lang, wording)
    select distinct id, lang, wording from(
        select
          convert(int, json_value(ext.value, '$.term.id')) as id
        --, json_query(ext.value, '$.term') as json
        , json_value(ext.value, '$.term.lang') as lang
        , json_value(ext.value, '$.term.wording') as wording
        from entries as e
        cross apply openjson(e.json, '$.desigs') as ext
        where e.id=@entryID
    ) as t

    delete from term_pos where term_id in (select id from @termIDs)
    insert into term_pos(term_id, pos_id)
    select distinct term_id, pos_id from(
        select
          convert(int, json_value(ext.value, '$.term.id')) as term_id
        , json_value(annot.value, '$.label.value') as pos_id
        from entries as e
        cross apply openjson(e.json, '$.desigs') as ext
        cross apply openjson(ext.value, '$.term.annots') as annot
        where e.id=@entryID
        and json_value(annot.value, '$.label.type')='posLabel'
    ) as t

    delete from words where term_id in (select id from @termIDs)
    insert into words(term_id, word)
    select t.id, dbo.spartanize(w.display_term)
    from terms as t
    cross apply (
        select display_term
        from sys.dm_fts_parser(N'"'+replace(t.wording, '"', ' ')+'"', 0, null, 1)
        where special_term in ('Noise Word', 'Exact Match')
    ) as w
    where t.id in (select id from @termIDs)

    delete from spelling where term_id in (select id from @termIDs)
    insert into spelling(term_id, word, [A],[B],[C],[D],[E],[F],[G],[H],[I],[J],[K],[L],[M],[N],[O],[P],[Q],[R],[S],[T],[U],[V],[W],[X],[Y],[Z], [length])
    select t.id, t.wording, [A],[B],[C],[D],[E],[F],[G],[H],[I],[J],[K],[L],[M],[N],[O],[P],[Q],[R],[S],[T],[U],[V],[W],[X],[Y],[Z], LEN(t.wording)
    from terms as t
    cross apply (
        select [A],[B],[C],[D],[E],[F],[G],[H],[I],[J],[K],[L],[M],[N],[O],[P],[Q],[R],[S],[T],[U],[V],[W],[X],[Y],[Z]
        from dbo.characterize(t.wording)
    ) as c
    where t.id in (select id from @termIDs)
    --and len(t.wording)<=10

    update entries set sortkeyGA=null, sortkeyEN=null where id=@entryID
    declare @temp table(entry_id int, sortkey nvarchar(max), listingOrder int)
    insert into @temp(entry_id, sortkey, listingOrder)
        select e.id, JSON_VALUE(pj.value, '$.term.wording'), pj.[key]
        from entries as e
        cross apply openjson(e.json, '$.desigs') as pj
        where e.id=@entryID and JSON_VALUE(pj.value, '$.term.lang')='ga'
        update e set e.sortkeyGA=t.sortkey
            from entries as e
            inner join @temp as t on t.entry_id=e.id
            where e.id=@entryID and e.sortkeyGA is null
    delete from @temp
    insert into @temp(entry_id, sortkey, listingOrder)
        select e.id, JSON_VALUE(pj.value, '$.term.wording'), pj.[key]
        from entries as e
        cross apply openjson(e.json, '$.desigs') as pj
        where e.id=@entryID and JSON_VALUE(pj.value, '$.term.lang')='en'
        update e set e.sortkeyEN=t.sortkey
            from entries as e
            inner join @temp as t on t.entry_id=e.id
            where e.id=@entryID and e.sortkeyEN is null

end
end
GO
/****** Object:  StoredProcedure [dbo].[propag_saveMetadatum]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[propag_saveMetadatum]
  @id int
, @type varchar(255)
, @json nvarchar(max)
, @collection nvarchar(10) = ''
as
begin

if (select count(*) from metadata where id=@id) = 0
begin
    insert into metadata(id, [type], [json]) values(@id, @type, @json)
end
else
begin
    update metadata set [type]=@type, [json]=@json where id=@id
end

update metadata set
  sortkeyGA=JSON_VALUE(json, '$.title.ga')
, sortkeyEN=JSON_VALUE(json, '$.title.en')
, parentID=JSON_VALUE(json, '$.parentID')
where id=@id

end
GO
/****** Object:  StoredProcedure [dbo].[pub_domain]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[pub_domain]
    @lang nvarchar(255),
    @domID int,
    @page int = 1,
    @total int output
as
begin
    set nocount on;

    -------------------------------------------------------------------------
    -- expand domain IDs
    -------------------------------------------------------------------------
    create table #domainIDs (domainID int);

    insert into #domainIDs(domainID)
        select domainID
        from dbo.expandDomainID_inline(@domID, default);

    create index ix_domainIDs on #domainIDs(domainID);

    -------------------------------------------------------------------------
    -- match entries
    -------------------------------------------------------------------------
    create table #matches (
        entry_id int,
        rownum int
    );
    create index ix_matches_entry on #matches(entry_id);
    create index ix_matches_rownum on #matches(rownum) include (entry_id);

    if @lang = 'ga'
    begin
        insert into #matches(entry_id, rownum)
            select e.id, row_number() over (order by e.sortkeyGA)
            from entries e
            join entry_domain ed on ed.entry_id = e.id
            join #domainIDs di on di.domainID = ed.superdomain
            where e.pStatus = 1;
    end
    else
    begin
        insert into #matches(entry_id, rownum)
            select e.id, row_number() over (order by e.sortkeyEN)
            from entries e
            join entry_domain ed on ed.entry_id = e.id
            join #domainIDs di on di.domainID = ed.superdomain
            where e.pStatus = 1;
    end

    -------------------------------------------------------------------------
    -- paging
    -------------------------------------------------------------------------
    declare @lastRow int = (select count(*) from #matches);
    declare @maxPage int = @lastRow / 100 + case when @lastRow % 100 > 0 then 1 else 0 end;

    if @page > @maxPage set @page = @maxPage;
    declare @firstRow int = (@page * 100) - 99;

    -------------------------------------------------------------------------
    -- xrefs
    -------------------------------------------------------------------------
    select trg.*
    from entries trg
    join entry_xref x on x.target_entry_id = trg.id
    join #matches src on src.entry_id = x.source_entry_id;

    -------------------------------------------------------------------------
    -- matches
    -------------------------------------------------------------------------
    select @total = count(*) from #matches;

    select top (100) e.*
    from entries e
    join #matches m on m.entry_id = e.id
    where m.rownum >= @firstRow
    order by m.rownum;

    -------------------------------------------------------------------------
    -- pager
    -------------------------------------------------------------------------
    select @page as currentPage,
           @maxPage as maxPage;
end
GO
/****** Object:  StoredProcedure [dbo].[pub_domains]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[pub_domains]
    @lang nvarchar(255)
as
begin
    set nocount on;

    -------------------------------------------------------------------------
    -- lingo
    -------------------------------------------------------------------------
    select *
    from configs
    where id = 'lingo';

    -------------------------------------------------------------------------
    -- domains (ga or en ordering)
    -------------------------------------------------------------------------
    if @lang = 'ga'
    begin
        select
            m.id,
            m.type,
            m.json,
            m.sortkeyGA as sortkey,
            count(ch.id) as hasChildren
        from metadata m
        left join metadata ch on ch.parentID = m.id
        where m.type = 'domain'
        group by m.id, m.type, m.json, m.sortkeyGA
        order by m.sortkeyGA;
    end
    else if @lang = 'en'
    begin
        select
            m.id,
            m.type,
            m.json,
            m.sortkeyEN as sortkey,
            count(ch.id) as hasChildren
        from metadata m
        left join metadata ch on ch.parentID = m.id
        where m.type = 'domain'
        group by m.id, m.type, m.json, m.sortkeyEN
        order by m.sortkeyEN;
    end
end

GO
/****** Object:  StoredProcedure [dbo].[pub_entry]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[pub_entry]
    @id int
as
begin
    set nocount on;

    -------------------------------------------------------------------------
    -- xref targets
    -------------------------------------------------------------------------
    select trg.*
    from entries trg
    join entry_xref x on x.target_entry_id = trg.id
    where x.source_entry_id = @id;

    -------------------------------------------------------------------------
    -- entry
    -------------------------------------------------------------------------
    select *
    from entries
    where pStatus = 1
      and id = @id;
end

GO
/****** Object:  StoredProcedure [dbo].[pub_index]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[pub_index]
as
begin
    set nocount on;

    -------------------------------------------------------------------------
    -- entry of the day
    -------------------------------------------------------------------------
    select top (1) id, json
    from entries
    where pStatus = 1
      and tod = convert(date, getdate())
    order by dateStamp desc, id desc;

    -------------------------------------------------------------------------
    -- recently changed entries
    -------------------------------------------------------------------------
    select top (20) id, json
    from entries
    where pStatus = 1
    order by dateStamp desc;

    -------------------------------------------------------------------------
    -- announcement
    -------------------------------------------------------------------------
    declare @now datetime = getdate();

    select
        json_value(json, '$.text.ga') as textGA,
        json_value(json, '$.text.en') as textEN
    from configs
    where id = 'news'
      and convert(date, json_value(json, '$.from.date')) <= convert(date, @now)
      and (json_value(json, '$.from.time') = ''
           or convert(time, json_value(json, '$.from.time')) <= convert(time, @now))
      and convert(date, json_value(json, '$.till.date')) >= convert(date, @now)
      and (json_value(json, '$.till.time') = ''
           or convert(time, json_value(json, '$.till.time')) >= convert(time, @now));
end
GO
/****** Object:  StoredProcedure [dbo].[pub_peek]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[pub_peek]
    @word nvarchar(255)
as
begin
    set nocount on;

    -------------------------------------------------------------------------
    -- preprocess input
    -------------------------------------------------------------------------
    declare @wordSpartanized nvarchar(255) = dbo.spartanize(@word);

    -------------------------------------------------------------------------
    -- exact matches
    -------------------------------------------------------------------------
    create table #exacts (entry_id int, lang nvarchar(10) collate Latin1_General_CI_AI);
    create clustered index ix_exacts on #exacts(entry_id, lang);

    insert into #exacts(entry_id, lang)
        select e.id, t.lang
        from entries e
        join entry_term et on et.entry_id = e.id
        join terms t on t.id = et.term_id
        where t.wordingSpartanized = @wordSpartanized
          and e.pStatus = 1;

    -------------------------------------------------------------------------
    -- tokenise input
    -------------------------------------------------------------------------
    create table #words (word nvarchar(255) collate Latin1_General_CI_AI);
    create index ix_words on #words(word);

    insert into #words(word)
    select dbo.spartanize(display_term)
    from sys.dm_fts_parser(N'"' + replace(replace(@word, '-', ' '), '"', ' ') + '"', 0, null, 1)
    where special_term in ('Noise Word', 'Exact Match')
    order by len(display_term) desc;

    -------------------------------------------------------------------------
    -- related matches
    -------------------------------------------------------------------------
    create table #relateds (
        entry_id int,
        lang nvarchar(10) collate Latin1_General_CI_AI,
        term_id int
    );
    create index ix_relateds on #relateds(entry_id);

    create table #tokens (
        token nvarchar(255) collate Latin1_General_CI_AS,
        lang  nvarchar(10) collate Latin1_General_CI_AI
    );
    create index ix_tokens on #tokens(token, lang);

    create table #temp (entry_id int, term_id int);
    create index ix_temp on #temp(entry_id);

    -------------------------------------------------------------------------
    -- first token
    -------------------------------------------------------------------------
    declare @word1 nvarchar(255);
    select top (1) @word1 = word from #words;

    if @word1 is not null and @word1 <> ''
    begin
        delete from #tokens;
        insert into #tokens(token, lang) values(@word1, '');

        insert into #tokens(token, lang)
            select token, lang from flex where lemma = @word1;

        insert into #tokens(token, lang)
            select lemma, lang from flex where token = @word1;

        insert into #relateds(entry_id, lang, term_id)
            select distinct e.id, t.lang, t.id
            from entries e
            join entry_term et on et.entry_id = e.id
            join terms t on t.id = et.term_id
            join words w on w.term_id = t.id
            join #tokens tok on tok.token = w.word
            where tok.lang = t.lang
            union all
            select distinct e.id, t.lang, t.id
            from entries e
            join entry_term et on et.entry_id = e.id
            join terms t on t.id = et.term_id
            join words w on w.term_id = t.id
            join #tokens tok on tok.token = w.word
            where tok.lang = '';
    end

    -------------------------------------------------------------------------
    -- second token
    -------------------------------------------------------------------------
    declare @word2 nvarchar(255);
    select top (2) @word2 = word from #words;

    if @word2 is not null and @word2 <> @word1
    begin
        delete from #temp;
        insert into #temp select entry_id, term_id from #relateds;

        delete from #relateds;
        delete from #tokens;

        insert into #tokens(token, lang) values(@word2, '');

        insert into #tokens(token, lang)
            select token, lang from flex where lemma = @word2;

        insert into #tokens(token, lang)
            select lemma, lang from flex where token = @word2;

        insert into #relateds(entry_id, lang, term_id)
            select distinct e.id, t.lang, t.id
            from entries e
            join #temp temp on temp.entry_id = e.id
            join entry_term et on et.entry_id = e.id
            join terms t on t.id = et.term_id
            join words w on w.term_id = t.id
            join #tokens tok on tok.token = w.word
            where tok.lang = t.lang
            union all
            select distinct e.id, t.lang, t.id
            from entries e
            join #temp temp on temp.entry_id = e.id
            join entry_term et on et.entry_id = e.id
            join terms t on t.id = et.term_id
            join words w on w.term_id = t.id
            join #tokens tok on tok.token = w.word
            where tok.lang = '';
    end

    -------------------------------------------------------------------------
    -- third token
    -------------------------------------------------------------------------
    declare @word3 nvarchar(255);
    select top (3) @word3 = word from #words;

    if @word3 is not null and @word3 <> @word2 and @word3 <> @word1
    begin
        delete from #temp;
        insert into #temp select entry_id, term_id from #relateds;

        delete from #relateds;
        delete from #tokens;

        insert into #tokens(token, lang) values(@word3, '');

        insert into #tokens(token, lang)
            select token, lang from flex where lemma = @word3;

        insert into #tokens(token, lang)
            select lemma, lang from flex where token = @word3;

        insert into #relateds(entry_id, lang, term_id)
            select distinct e.id, t.lang, t.id
            from entries e
            join #temp temp on temp.entry_id = e.id
            join entry_term et on et.entry_id = e.id
            join terms t on t.id = et.term_id
            join words w on w.term_id = t.id
            join #tokens tok on tok.token = w.word
            where tok.lang = t.lang
            union all
            select distinct e.id, t.lang, t.id
            from entries e
            join #temp temp on temp.entry_id = e.id
            join entry_term et on et.entry_id = e.id
            join terms t on t.id = et.term_id
            join words w on w.term_id = t.id
            join #tokens tok on tok.token = w.word
            where tok.lang = '';
    end

    -------------------------------------------------------------------------
    -- return counts
    -------------------------------------------------------------------------
    declare @count_exacts int =
        (select count(distinct entry_id) from #exacts);

    declare @count_relateds int =
        (select count(distinct r.entry_id)
         from #relateds r
         where not exists (select 1 from #exacts ex where ex.entry_id = r.entry_id));

    select @count_exacts as countExacts,
           @count_relateds as countRelateds;
end
GO
/****** Object:  StoredProcedure [dbo].[pub_quicksearch]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[pub_quicksearch]
      @word nvarchar(255)
    , @lang nvarchar(10) = '' 
    , @super bit = 0
as
begin
    set nocount on;

    -------------------------------------------------------------------------
    -- Preprocess input
    -------------------------------------------------------------------------
    declare @wordSpartanized nvarchar(255);
    set @wordSpartanized = dbo.spartanize(@word);

    -------------------------------------------------------------------------
    -- SIMILAR TERMS (cached in search_text / similar)
    -------------------------------------------------------------------------
    if (@word = '')
    begin
        select top (0) *
        from spelling;
    end
    else
    begin
        ---------------------------------------------------------------------
        -- Ensure a single searchTextID exists for this @word (concurrency-safe)
        ---------------------------------------------------------------------
        declare @searchtextId int;
        declare @newId table (id int);

        ;with src as (
            select @word as searchText
        )
        merge search_text with (holdlock) as t
        using src
            on t.searchText = src.searchText
        when not matched then
            insert (searchText)
            values (src.searchText)
        output inserted.id into @newId;

        select @searchtextId = id
        from @newId;

        if (@searchtextId is null)
        begin
            select @searchtextId = ID
            from search_text
            where searchText = @word;
        end

        ---------------------------------------------------------------------
        -- Only compute similars if not already cached
        ---------------------------------------------------------------------
        if not exists (
            select 1
            from similar st
            where st.searchTextID = @searchtextId
        )
        begin
            -----------------------------------------------------------------
            -- Prepare A–Z vector for similarity matching
            -----------------------------------------------------------------
            declare @searchtextStart nvarchar(1) = substring(@word, 1, 1);
            declare @maxWordLength int = len(@word) + 5;
            declare @minWordLength int = len(@word) - 5;

            declare @chars table (
                A int, B int, C int, D int, E int,
                F int, G int, H int, I int, J int,
                K int, L int, M int, N int, O int,
                P int, Q int, R int, S int, T int,
                U int, V int, W int, X int, Y int,
                Z int
            );

            insert into @chars
                select [A],[B],[C],[D],[E],[F],[G],[H],[I],[J],
                       [K],[L],[M],[N],[O],[P],[Q],[R],[S],[T],
                       [U],[V],[W],[X],[Y],[Z]
                from dbo.characterize_inline(@word);

            -----------------------------------------------------------------
            -- STRICT CANDIDATES (first_char + length + A–Z bounds)
            -----------------------------------------------------------------
            declare @Strict table (
                word nvarchar(255),
                diffAZ int
            );

            ;with StrictCandidates as (
                select top (50)
                    s.word,
                    abs(s.A - c.A) + abs(s.B - c.B) + abs(s.C - c.C) + abs(s.D - c.D) + abs(s.E - c.E)
                    + abs(s.F - c.F) + abs(s.G - c.G) + abs(s.H - c.H) + abs(s.I - c.I) + abs(s.J - c.J)
                    + abs(s.K - c.K) + abs(s.L - c.L) + abs(s.M - c.M) + abs(s.N - c.N) + abs(s.O - c.O)
                    + abs(s.P - c.P) + abs(s.Q - c.Q) + abs(s.R - c.R) + abs(s.S - c.S) + abs(s.T - c.T)
                    + abs(s.U - c.U) + abs(s.V - c.V) + abs(s.W - c.W) + abs(s.X - c.X) + abs(s.Y - c.Y)
                    + abs(s.Z - c.Z) as diffAZ
                from spelling s
                cross join @chars c
                where s.first_char = @searchtextStart
                  and s.[length] between @minWordLength and @maxWordLength
                  and s.word <> @word
                  and s.A between c.A - 2 and c.A + 2
                  and s.B between c.B - 2 and c.B + 2
                  and s.C between c.C - 2 and c.C + 2
                  and s.D between c.D - 2 and c.D + 2
                  and s.E between c.E - 2 and c.E + 2
                  and s.F between c.F - 2 and c.F + 2
                  and s.G between c.G - 2 and c.G + 2
                  and s.H between c.H - 2 and c.H + 2
                  and s.I between c.I - 2 and c.I + 2
                  and s.J between c.J - 2 and c.J + 2
                  and s.K between c.K - 2 and c.K + 2
                  and s.L between c.L - 2 and c.L + 2
                  and s.M between c.M - 2 and c.M + 2
                  and s.N between c.N - 2 and c.N + 2
                  and s.O between c.O - 2 and c.O + 2
                  and s.P between c.P - 2 and c.P + 2
                  and s.Q between c.Q - 2 and c.Q + 2
                  and s.R between c.R - 2 and c.R + 2
                  and s.S between c.S - 2 and c.S + 2
                  and s.T between c.T - 2 and c.T + 2
                  and s.U between c.U - 2 and c.U + 2
                  and s.V between c.V - 2 and c.V + 2
                  and s.W between c.W - 2 and c.W + 2
                  and s.X between c.X - 2 and c.X + 2
                  and s.Y between c.Y - 2 and c.Y + 2
                  and s.Z between c.Z - 2 and c.Z + 2
                order by diffAZ asc
            )
            insert into @Strict(word, diffAZ)
                select word, diffAZ
                from StrictCandidates;

            declare @strictCount int = (select count(*) from @Strict);

            -----------------------------------------------------------------
            -- LOOSE CANDIDATES (no first_char filter) if strict too small
            -----------------------------------------------------------------
            declare @Loose table (
                word nvarchar(255),
                diffAZ int
            );

            if (@strictCount < 10)
            begin
                ;with LooseCandidates as (
                    select top (50)
                        s.word,
                        abs(s.A - c.A) + abs(s.B - c.B) + abs(s.C - c.C) + abs(s.D - c.D) + abs(s.E - c.E)
                        + abs(s.F - c.F) + abs(s.G - c.G) + abs(s.H - c.H) + abs(s.I - c.I) + abs(s.J - c.J)
                        + abs(s.K - c.K) + abs(s.L - c.L) + abs(s.M - c.M) + abs(s.N - c.N) + abs(s.O - c.O)
                        + abs(s.P - c.P) + abs(s.Q - c.Q) + abs(s.R - c.R) + abs(s.S - c.S) + abs(s.T - c.T)
                        + abs(s.U - c.U) + abs(s.V - c.V) + abs(s.W - c.W) + abs(s.X - c.X) + abs(s.Y - c.Y)
                        + abs(s.Z - c.Z) as diffAZ
                    from spelling s
                    cross join @chars c
                    where s.[length] between @minWordLength and @maxWordLength
                      and s.word <> @word
                      and s.A between c.A - 2 and c.A + 2
                      and s.B between c.B - 2 and c.B + 2
                      and s.C between c.C - 2 and c.C + 2
                      and s.D between c.D - 2 and c.D + 2
                      and s.E between c.E - 2 and c.E + 2
                      and s.F between c.F - 2 and c.F + 2
                      and s.G between c.G - 2 and c.G + 2
                      and s.H between c.H - 2 and c.H + 2
                      and s.I between c.I - 2 and c.I + 2
                      and s.J between c.J - 2 and c.J + 2
                      and s.K between c.K - 2 and c.K + 2
                      and s.L between c.L - 2 and c.L + 2
                      and s.M between c.M - 2 and c.M + 2
                      and s.N between c.N - 2 and c.N + 2
                      and s.O between c.O - 2 and c.O + 2
                      and s.P between c.P - 2 and c.P + 2
                      and s.Q between c.Q - 2 and c.Q + 2
                      and s.R between c.R - 2 and c.R + 2
                      and s.S between c.S - 2 and c.S + 2
                      and s.T between c.T - 2 and c.T + 2
                      and s.U between c.U - 2 and c.U + 2
                      and s.V between c.V - 2 and c.V + 2
                      and s.W between c.W - 2 and c.W + 2
                      and s.X between c.X - 2 and c.X + 2
                      and s.Y between c.Y - 2 and c.Y + 2
                      and s.Z between c.Z - 2 and c.Z + 2
                    order by diffAZ asc
                )
                insert into @Loose(word, diffAZ)
                    select word, diffAZ
                    from LooseCandidates;
            end

            -----------------------------------------------------------------
            -- FINAL SIMILAR SET: dedupe, compute Levenshtein, apply <= 4
            -----------------------------------------------------------------
            declare @src table(word nvarchar(255), diff int);

            ;with u as (
                select word, src,
                       row_number() over (partition by word order by src) as rn
                from (
                    select word, 1 as src from @Strict
                    union all
                    select word, 2 as src from @Loose where @strictCount < 10
                ) as q
            )
            insert into @src(word, diff)
                select top (7)
                    word,
                    case
                        when word = @word collate Latin1_General_CI_AI then 0
                        else d.dist
                    end
                from u
                cross apply (select dbo.levenshtein(word, @word) as dist) d
                where rn = 1
                  and d.dist <= 4
                order by 2;

            -----------------------------------------------------------------
            -- Merge similars into cache
            -----------------------------------------------------------------
            merge similar with (holdlock) as target
            using @src as src
                on target.searchTextID = @searchtextId
               and target.similar = src.word collate Latin1_General_CI_AS
            when not matched then
                insert (searchTextID, similar, diff)
                values (@searchtextId, src.word, src.diff);
        end
    
        ---------------------------------------------------------------------
        -- Return similars
        ---------------------------------------------------------------------
        select
            similar,
            diff
        from similar
        where searchTextID = @searchtextId
        order by diff;
    end

    -------------------------------------------------------------------------
    -- EXACT MATCHES
    -------------------------------------------------------------------------
    create table #exacts (entry_id int, lang nvarchar(10) collate Latin1_General_CI_AI);
    create clustered index IX_exacts on #exacts(entry_id, lang);

    insert into #exacts(entry_id, lang)
        select e.id, t.lang
        from entries as e
        inner join entry_term as et on et.entry_id = e.id
        inner join terms as t on t.id = et.term_id
        where t.wordingSpartanized = @wordSpartanized
          and e.pStatus = 1;

    -------------------------------------------------------------------------
    -- TOKENISE INPUT (FTS parser) FOR RELATED MATCHES
    -------------------------------------------------------------------------
    create table #words (word nvarchar(255) collate Latin1_General_CI_AI);
    create index IX_words on #words(word);

    insert into #words(word)
        select dbo.spartanize(display_term)
        from sys.dm_fts_parser(N'"' + replace(replace(@word, '-', ' '), '"', ' ') + '"', 0, null, 1)
        where special_term in ('Noise Word', 'Exact Match')
        order by len(display_term) desc;

    -------------------------------------------------------------------------
    -- RELATED MATCHES (flex-expanded token matching)
    -------------------------------------------------------------------------
    create table #relateds (entry_id int, lang nvarchar(10) collate Latin1_General_CI_AI, term_id int);
    create index IX_relateds on #relateds(entry_id, lang);

    create table #tokens (
        token nvarchar(255) collate Latin1_General_CI_AS,
        lang nvarchar(10) collate Latin1_General_CI_AI
    );
    create index IX_tokens on #tokens(token, lang);
    
    declare @temp table(entry_id int, term_id int);

    -------------------------------------------------------------------------
    -- FIRST TOKEN
    -------------------------------------------------------------------------
    declare @word1 nvarchar(255);
    
    select top (1) @word1 = word
    from #words;

    if (@word1 is not null and @word1 <> '')
    begin
        delete from #tokens;
        insert into #tokens(token, lang) values(@word1, '');

        insert into #tokens(token, lang)
            select token, lang
            from flex
            where lemma = @word1;
            
        insert into #tokens(token, lang)
            select lemma, lang
            from flex
            where token = @word1;

        insert into #relateds(entry_id, lang, term_id)
            -- branch 1: language-specific
            select e.id, t.lang, t.id
            from entries as e
            inner join entry_term as et on et.entry_id = e.id
            inner join terms as t on t.id = et.term_id
            inner join words as w on w.term_id = t.id
            inner join #tokens as tok on tok.token = w.word
            where tok.lang = t.lang
            union all
            -- branch 2: language-agnostic
            select e.id, t.lang, t.id
            from entries e
            inner join entry_term et on et.entry_id = e.id
            inner join terms t on t.id = et.term_id
            inner join words w on w.term_id = t.id
            inner join #tokens tok on tok.token = w.word
            where tok.lang = '';
    end

    -------------------------------------------------------------------------
    -- SECOND TOKEN
    -------------------------------------------------------------------------
    declare @word2 nvarchar(255);

    select top (2) @word2 = word
    from #words;
    
    if (@word2 is not null and @word2 <> @word1)
    begin
        delete from @temp;

        insert into @temp(entry_id, term_id)
            select entry_id, term_id
            from #relateds;
            
        delete from #relateds;
        
        delete from #tokens;
        insert into #tokens(token, lang) values(@word2, '');
        
        insert into #tokens(token, lang)
            select token, lang
            from flex
            where lemma = @word2;

        insert into #tokens(token, lang)
            select lemma, lang
            from flex
            where token = @word2;

        insert into #relateds(entry_id, lang, term_id)
            select e.id, t.lang, t.id
            from entries as e
            inner join @temp as temp on temp.entry_id = e.id
            inner join entry_term as et on et.entry_id = e.id
            inner join terms as t on t.id = et.term_id
            inner join words as w on w.term_id = t.id
            inner join #tokens as tok on tok.token = w.word
            where tok.lang = t.lang
            union all
            select e.id, t.lang, t.id
            from entries as e
            inner join @temp as temp on temp.entry_id = e.id
            inner join entry_term as et on et.entry_id = e.id
            inner join terms as t on t.id = et.term_id
            inner join words as w on w.term_id = t.id
            inner join #tokens as tok on tok.token = w.word
            where tok.lang = '';
    end

    -------------------------------------------------------------------------
    -- THIRD TOKEN
    -------------------------------------------------------------------------
    declare @word3 nvarchar(255);

    select top (3) @word3 = word
    from #words;

    if (@word3 is not null and @word3 <> @word2 and @word3 <> @word1)
    begin
        delete from @temp;

        insert into @temp(entry_id, term_id)
            select entry_id, term_id
            from #relateds;
            
        delete from #relateds;

        delete from #tokens;
        insert into #tokens(token, lang) values(@word3, '');
        
        insert into #tokens(token, lang)
            select token, lang
            from flex
            where lemma = @word3;
            
        insert into #tokens(token, lang)
            select lemma, lang
            from flex
            where token = @word3;

        insert into #relateds(entry_id, lang, term_id)
            select e.id, t.lang, t.id
            from entries as e
            inner join @temp as temp on temp.entry_id = e.id
            inner join entry_term as et on et.entry_id = e.id
            inner join terms as t on t.id = et.term_id
            inner join words as w on w.term_id = t.id
            inner join #tokens as tok on tok.token = w.word
            where tok.lang = t.lang
            union all
            select e.id, t.lang, t.id
            from entries as e
            inner join @temp as temp on temp.entry_id = e.id
            inner join entry_term as et on et.entry_id = e.id
            inner join terms as t on t.id = et.term_id
            inner join words as w on w.term_id = t.id
            inner join #tokens as tok on tok.token = w.word
            where tok.lang = '';
    end

    -------------------------------------------------------------------------
    -- RETURN LANGUAGES FOUND IN EXACT OR RELATED MATCHES
    -------------------------------------------------------------------------
    select t.lang
    from (
        select entry_id, lang
        from #exacts
        union
        select entry_id, lang
        from #relateds
    ) as t
    group by lang
    order by count(*) desc;

    -------------------------------------------------------------------------
    -- DETERMINE SORT LANGUAGE
    -------------------------------------------------------------------------
    declare @sortlang nvarchar(10);
    set @sortlang = @lang;
    
    if (@sortlang = '')
    begin
        select top (1) @sortlang = t.lang
        from (
            select entry_id, lang
            from #exacts
            union
            select entry_id, lang
            from #relateds
        ) as t
        group by lang
        order by count(*) desc;
    end

    -------------------------------------------------------------------------
    -- RETURN XREF TARGETS
    -------------------------------------------------------------------------
    select *
    from entries as trg
    inner join entry_xref as x on x.target_entry_id = trg.id
    inner join (
        select entry_id from #exacts
        union
        select entry_id from #relateds
    ) as src on src.entry_id = x.source_entry_id;

    if (@lang = '')
    begin
        select distinct e.*, case when @sortlang = 'en' then e.sortkeyEN else e.sortkeyGA end as sortkey
        from entries as e
        inner join #exacts as ids on ids.entry_id = e.id
        order by sortkey, e.id;

        select distinct top (101) e.*, case when @sortlang = 'en' then e.sortkeyEN else e.sortkeyGA end as sortkey
        from entries as e
        inner join #relateds as ids on ids.entry_id = e.id
        where not exists (select 1 from #exacts as ex where ex.entry_id = e.id)
            and e.pStatus = 1
        order by sortkey, e.id;
    end
    else
    begin
        select distinct e.*, case when @sortlang = 'en' then e.sortkeyEN else e.sortkeyGA end as sortkey
        from entries as e
        inner join #exacts as ids on ids.entry_id = e.id
        where ids.lang = @lang
        order by sortkey, e.id;

        select distinct top (101) e.*, case when @sortlang = 'en' then e.sortkeyEN else e.sortkeyGA end as sortkey
        from entries as e
        inner join #relateds as ids on ids.entry_id = e.id
        where ids.lang = @lang
            and not exists (select 1 from #exacts as ex where ex.entry_id = e.id)
            and e.pStatus = 1
        order by sortkey, e.id;
    end

    -------------------------------------------------------------------------
    -- AUXILIARY RESULTS (for super.tearma.ie)
    -------------------------------------------------------------------------
    if (@super = 1)
    begin
        select top (500) *
        from aux
        where en like '%' + @word + '%' or ga like '%' + @word + '%'
        order by coll, id;
    end
end
GO
/****** Object:  StoredProcedure [dbo].[pub_subdoms]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[pub_subdoms]
  @domID int
as
begin

--return lingo and metadata:
select * from configs where id = 'lingo';
select *, 0 as hasChildren from metadata where type = 'domain' and id = @domID;

end
GO
/****** Object:  StoredProcedure [dbo].[pub_tod]    Script Date: 01/04/2026 17:13:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[pub_tod]
as
begin
    set nocount on;

    declare @today date = CAST(GETDATE() AS date);

    select top 1 id, json
    from entries
    where pStatus = 1
      and tod = @today
    order by dateStamp desc, id desc;
end

GO
