/****** Object:  UserDefinedFunction [dbo].[characterize]    Script Date: 16/01/2019 18:41:28 ******/
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
/****** Object:  UserDefinedFunction [dbo].[levenshtein]    Script Date: 16/01/2019 18:41:28 ******/
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
/****** Object:  UserDefinedFunction [dbo].[min3]    Script Date: 16/01/2019 18:41:28 ******/
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
/****** Object:  UserDefinedFunction [dbo].[substrings]    Script Date: 16/01/2019 18:41:28 ******/
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
/****** Object:  UserDefinedFunction [dbo].[spartanize]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create function [dbo].[spartanize](@text nvarchar(255))
returns nvarchar(255)
with schemabinding
begin
	set @text=LOWER(@text);

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
/****** Object:  Table [dbo].[chars]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[chars](
	[base] [nvarchar](1) NOT NULL,
	[variant] [nvarchar](1) NOT NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[configs]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[configs](
	[id] [nvarchar](255) NOT NULL,
	[json] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[entries]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[entries](
	[id] [int] NOT NULL,
	[json] [nvarchar](max) NULL,
	[cStatus] [int] NULL,
	[pStatus] [int] NULL,
	[dateStamp] [date] NULL,
	[sortkeyGA] [nvarchar](255) NULL,
	[sortkeyEN] [nvarchar](255) NULL,
 CONSTRAINT [PK__entries__3213E83FB84A5A39] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[entry_domain]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[entry_domain](
	[entry_id] [int] NULL,
	[superdomain] [int] NULL,
	[subdomain] [int] NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[entry_term]    Script Date: 16/01/2019 18:41:28 ******/
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
/****** Object:  Table [dbo].[flex]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[flex](
	[term_id] [int] NULL,
	[lang] [nvarchar](10) NOT NULL,
	[lemma] [nvarchar](255) NOT NULL,
	[token] [nvarchar](255) NOT NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[metadata]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[metadata](
	[id] [int] NOT NULL,
	[type] [varchar](255) NULL,
	[json] [nvarchar](max) NULL,
	[sortkeyGA] [nvarchar](max) NULL,
	[sortkeyEN] [nvarchar](max) NULL,
 CONSTRAINT [PK__metadata__3213E83FD56CDBC2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[spelling]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[spelling](
	[term_id] [int] NOT NULL,
	[word] [nvarchar](255) NOT NULL,
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
	[length] [int] NOT NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[terms]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[terms](
	[id] [int] NOT NULL,
	[json] [nvarchar](max) NULL,
	[lang] [nvarchar](10) NULL,
	[wording] [nvarchar](255) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[words]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[words](
	[term_id] [int] NULL,
	[word] [nvarchar](255) NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_entries_sortkeyEN]    Script Date: 16/01/2019 18:41:28 ******/
CREATE NONCLUSTERED INDEX [IX_entries_sortkeyEN] ON [dbo].[entries]
(
	[sortkeyEN] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_entries_sortkeyGA]    Script Date: 16/01/2019 18:41:28 ******/
CREATE NONCLUSTERED INDEX [IX_entries_sortkeyGA] ON [dbo].[entries]
(
	[sortkeyGA] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_entry_domain]    Script Date: 16/01/2019 18:41:28 ******/
CREATE NONCLUSTERED INDEX [IX_entry_domain] ON [dbo].[entry_domain]
(
	[subdomain] ASC,
	[superdomain] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_entry_term_1]    Script Date: 16/01/2019 18:41:28 ******/
CREATE NONCLUSTERED INDEX [IX_entry_term_1] ON [dbo].[entry_term]
(
	[entry_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_entry_term_2]    Script Date: 16/01/2019 18:41:28 ******/
CREATE NONCLUSTERED INDEX [IX_entry_term_2] ON [dbo].[entry_term]
(
	[term_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_flex_lemma]    Script Date: 16/01/2019 18:41:28 ******/
CREATE NONCLUSTERED INDEX [IX_flex_lemma] ON [dbo].[flex]
(
	[lemma] ASC,
	[lang] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_flex_token]    Script Date: 16/01/2019 18:41:28 ******/
CREATE NONCLUSTERED INDEX [IX_flex_token] ON [dbo].[flex]
(
	[token] ASC,
	[lang] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_spelling_length]    Script Date: 16/01/2019 18:41:28 ******/
CREATE NONCLUSTERED INDEX [IX_spelling_length] ON [dbo].[spelling]
(
	[length] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_terms_1]    Script Date: 16/01/2019 18:41:28 ******/
CREATE NONCLUSTERED INDEX [IX_terms_1] ON [dbo].[terms]
(
	[id] ASC,
	[lang] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_terms_2]    Script Date: 16/01/2019 18:41:28 ******/
CREATE NONCLUSTERED INDEX [IX_terms_2] ON [dbo].[terms]
(
	[wording] ASC,
	[lang] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_words_1]    Script Date: 16/01/2019 18:41:28 ******/
CREATE NONCLUSTERED INDEX [IX_words_1] ON [dbo].[words]
(
	[term_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_words_2]    Script Date: 16/01/2019 18:41:28 ******/
CREATE NONCLUSTERED INDEX [IX_words_2] ON [dbo].[words]
(
	[word] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  StoredProcedure [dbo].[pub_advsearch]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[pub_advsearch]
  @word nvarchar(255)
, @length nvarchar(2)
, @extent nvarchar(2)
, @lang nvarchar(255) --empty string means any language
, @page int = 1
as
begin

--return lingo and metadata:
select * from configs where id='lingo'
select * from metadata where type in ('acceptLabel', 'inflectLabel', 'posLabel', 'domain')

--get all matching entry IDs:
declare @matches table(entry_id int, rownum int)
if(@extent<>'ft')
begin
	insert into @matches(entry_id, rownum)
	select distinct e.id, ROW_NUMBER() over(order by case when @lang='en' then e.sortkeyEN else e.sortkeyGA end)
	from entries as e
	inner join entry_term as et on et.entry_id=e.id
	inner join terms as t on t.id=et.term_id
	where (@lang='' or @lang=t.lang)
	and e.pStatus=1
	and (
		   (@length='sw' and t.wording not like '% %')
		or (@length='mw' and t.wording like '% %')
		or (@length='al')
	)
	and (
		   (@extent='al' and t.wording=@word)
		or (@extent='st' and t.wording like @word+'%')
		or (@extent='ed' and t.wording like '%'+@word)
		or (@extent='pt' and t.wording like '%'+@word+'%')
		or (@extent='md' and t.wording like '_%'+@word+'%_')
	)
end
else
begin
	declare @words table(word nvarchar(max))
	insert into @words(word) select display_term
		from sys.dm_fts_parser(N'"'+replace(@word, '"', ' ')+'"', 0, null, 1)
		where special_term in ('Noise Word', 'Exact Match')
	declare @relateds table(entry_id int, lang nvarchar(10), term_id int)
	declare @temp table(entry_id int, term_id int)
	declare @tokens table(token nvarchar(255), lang nvarchar(10))
	--first word:
	declare @word1 nvarchar(max); select top 1 @word1=word from @words
	delete from @tokens; insert into @tokens(token, lang) values(@word1, ''); insert into @tokens(token, lang) select token, lang from flex where lemma=@word1
	insert into @relateds(entry_id, lang, term_id) select distinct e.id, t.lang, t.id from entries as e
		inner join entry_term as et on et.entry_id=e.id
		inner join terms as t on t.id=et.term_id
		inner join words as w on w.term_id=t.id and w.word in (select token collate Latin1_General_CI_AS from @tokens)
		inner join @tokens as tok on tok.token=w.word collate Latin1_General_CI_AS and (tok.lang=t.lang or tok.lang='')
		where (@lang='' or @lang=t.lang)
		and (
			   (@length='sw' and t.wording not like '% %')
			or (@length='mw' and t.wording like '% %')
			or (@length='al')
		)
	--second word:
	declare @word2 nvarchar(max); select top 2 @word2=word from @words
	if(@word2<>@word1)
	begin
		delete from @temp; insert into @temp(entry_id, term_id) select entry_id, term_id from @relateds; delete from @relateds
		delete from @tokens; insert into @tokens(token, lang) values(@word2, ''); insert into @tokens(token, lang) select token, lang from flex where lemma=@word2
		insert into @relateds(entry_id, lang, term_id) select distinct e.id, t.lang, t.id from entries as e
			inner join @temp as temp on temp.entry_id=e.id
			inner join entry_term as et on et.entry_id=e.id
			inner join terms as t on t.id=et.term_id
			inner join words as w on w.term_id=t.id and w.word in (select token collate Latin1_General_CI_AS from @tokens)
			inner join @tokens as tok on tok.token=w.word collate Latin1_General_CI_AS and (tok.lang=t.lang or tok.lang='')
			where (@lang='' or @lang=t.lang) and exists( select * from @temp as temp where temp.term_id=t.id and temp.entry_id=e.id )
	end
	--third word:
	declare @word3 nvarchar(max); select top 3 @word3=word from @words
	if(@word3<>@word2 and @word3<>@word1)
	begin
		delete from @temp; insert into @temp(entry_id, term_id) select entry_id, term_id from @relateds; delete from @relateds
		delete from @tokens; insert into @tokens(token, lang) values(@word3, ''); insert into @tokens(token, lang) select token, lang from flex where lemma=@word3
		insert into @relateds(entry_id, lang, term_id) select distinct e.id, t.lang, t.id from entries as e
			inner join @temp as temp on temp.entry_id=e.id
			inner join entry_term as et on et.entry_id=e.id
			inner join terms as t on t.id=et.term_id
			inner join words as w on w.term_id=t.id and w.word in (select token collate Latin1_General_CI_AS from @tokens)
			inner join @tokens as tok on tok.token=w.word collate Latin1_General_CI_AS and (tok.lang=t.lang or tok.lang='')
			where (@lang='' or @lang=t.lang) and exists( select * from @temp as temp where temp.term_id=t.id and temp.entry_id=e.id )
end
	--return:
	insert into @matches(entry_id, rownum)
	select distinct e.id, ROW_NUMBER() over(order by case when @lang='en' then e.sortkeyEN else e.sortkeyGA end)
	from entries as e
	inner join @relateds as ids on ids.entry_id=e.id
end

--compute paging data:
declare @lastRow int; select @lastRow=count(*) from @matches
declare @maxPage int; set @maxPage=@lastRow/100; if(@lastRow%100 > 0) set @maxPage=@maxPage+1
if(@page>@maxPage) set @page=@maxPage
declare @firstRow int; set @firstRow=(@page*100)-99

--return matches:
select top 100 e.*
from entries as e
inner join @matches as m on m.entry_id=e.id
where m.rownum>=@firstRow
order by m.rownum

--return pager:
select @page as [currentPage], @maxPage as [maxPage]

end
GO
/****** Object:  StoredProcedure [dbo].[pub_advsearch_prepare]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create procedure [dbo].[pub_advsearch_prepare]
as
begin

--return lingo and metadata:
select * from configs where id='lingo'
select top 0 * from metadata

end
GO
/****** Object:  StoredProcedure [dbo].[pub_domain]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[pub_domain]
  @lang nvarchar(255) --empty string means any language
, @domID int
, @subdomID int --zero means any
, @page int = 1
as
begin

--return lingo and metadata:
select * from configs where id='lingo'
select * from metadata where type in ('acceptLabel', 'inflectLabel', 'posLabel', 'domain') order by sortkeyGA

--get all matching entry IDs:
declare @matches table(entry_id int, rownum int)
insert into @matches(entry_id, rownum)
	select distinct e.id, ROW_NUMBER() over(order by case when @lang='en' then e.sortkeyEN else e.sortkeyGA end)
	from entries as e
	inner join entry_domain as ed on ed.entry_id=e.id
	where ed.superdomain=@domID and (@subdomID=0 or ed.subdomain=@subdomID) and e.pStatus=1

--compute paging data:
declare @lastRow int; select @lastRow=count(*) from @matches
declare @maxPage int; set @maxPage=@lastRow/100; if(@lastRow%100 > 0) set @maxPage=@maxPage+1
if(@page>@maxPage) set @page=@maxPage
declare @firstRow int; set @firstRow=(@page*100)-99

--return matches:
select top 100 e.*
from entries as e
inner join @matches as m on m.entry_id=e.id
where m.rownum>=@firstRow
order by m.rownum

--return pager:
select @page as [currentPage], @maxPage as [maxPage]

end
GO
/****** Object:  StoredProcedure [dbo].[pub_domains]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[pub_domains]
  @lang nvarchar(255)
as
begin

--return lingo and metadata:
select * from configs where id='lingo'
if(@lang='en') select * from metadata where [type]='domain' order by sortkeyEN else select * from metadata where [type]='domain' order by sortkeyGA

end
GO
/****** Object:  StoredProcedure [dbo].[pub_index]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[pub_index]
as
begin

--return lingo and metadata:
select * from configs where id='lingo'
select * from metadata where type in ('acceptLabel', 'inflectLabel', 'posLabel', 'domain') order by sortkeyGA

end
GO
/****** Object:  StoredProcedure [dbo].[pub_quicksearch]    Script Date: 16/01/2019 18:41:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[pub_quicksearch]
  @word nvarchar(255)
, @lang nvarchar(10) = '' --empty string means any language
as
begin

--return lingo and metadata:
select * from configs where id='lingo'
select * from metadata where type in ('acceptLabel', 'inflectLabel', 'posLabel', 'domain')

--return similars:
--select top 0 word as similar from words
select distinct top 5
  t.word as similar
, case when t.word=@word collate Latin1_General_CI_AI then 0
       else convert(int, dbo.levenshtein(t.word, @word))
  end as Diff
from (
	select top 50 s.word
		 , abs(s.A-c.A)+abs(s.B-c.B)+abs(s.C-c.C)+abs(s.D-c.D)+abs(s.E-c.E)+abs(s.F-c.F)
		  +abs(s.G-c.G)+abs(s.H-c.H)+abs(s.I-c.I)+abs(s.J-c.J)+abs(s.K-c.K)+abs(s.L-c.L)
		  +abs(s.M-c.M)+abs(s.N-c.N)+abs(s.O-c.O)+abs(s.P-c.P)+abs(s.Q-c.Q)+abs(s.R-c.R)
		  +abs(s.S-c.S)+abs(s.T-c.T)+abs(s.U-c.U)+abs(s.V-c.V)+abs(s.W-c.W)+abs(s.X-c.X)
		  +abs(s.Y-c.Y)+abs(s.Z-c.Z) as Diff
		, case when substring(@word,1,1)=substring(s.word,1,1) then 0 else 1 end as Start
	from spelling as s
	inner join dbo.characterize(@word) as c on 1=1
	where s.[length]<(LEN(@word)+5) and s.[length]>(LEN(@word)-5)
	and s.word<>@word
	order by Diff asc, Start asc
) as t
where convert(int, dbo.levenshtein(t.word, @word))<=4
order by Diff

--find exact matches:
declare @exacts table(entry_id int, lang nvarchar(10))
insert into @exacts(entry_id, lang) select e.id, t.lang from entries as e
	inner join entry_term as et on et.entry_id=e.id
	inner join terms as t on t.id=et.term_id
	where t.wording=@word and e.pStatus=1

--find related matches:
declare @words table(word nvarchar(max))
insert into @words(word) select display_term
	from sys.dm_fts_parser(N'"'+replace(@word, '"', ' ')+'"', 0, null, 1)
	where special_term in ('Noise Word', 'Exact Match')
declare @relateds table(entry_id int, lang nvarchar(10), term_id int)
declare @temp table(entry_id int, term_id int)
declare @tokens table(token nvarchar(255), lang nvarchar(10))
--first word:
declare @word1 nvarchar(max); select top 1 @word1=word from @words;
delete from @tokens; insert into @tokens(token, lang) values(@word1, ''); insert into @tokens(token, lang) select token, lang from flex where lemma=@word1
insert into @relateds(entry_id, lang, term_id) select distinct e.id, t.lang, t.id from entries as e
	inner join entry_term as et on et.entry_id=e.id
	inner join terms as t on t.id=et.term_id
	inner join words as w on w.term_id=t.id
	inner join @tokens as tok on tok.token=w.word collate Latin1_General_CI_AS and (tok.lang=t.lang or tok.lang='')
--second word:
declare @word2 nvarchar(max); select top 2 @word2=word from @words
if(@word2<>@word1)
begin
	delete from @temp; insert into @temp(entry_id, term_id) select entry_id, term_id from @relateds; delete from @relateds
	delete from @tokens; insert into @tokens(token, lang) values(@word2, ''); insert into @tokens(token, lang) select token, lang from flex where lemma=@word2
	insert into @relateds(entry_id, lang, term_id) select distinct e.id, t.lang, t.id from entries as e
		inner join @temp as temp on temp.entry_id=e.id
		inner join entry_term as et on et.entry_id=e.id
		inner join terms as t on t.id=et.term_id
		inner join words as w on w.term_id=t.id
		inner join @tokens as tok on tok.token=w.word collate Latin1_General_CI_AS and (tok.lang=t.lang or tok.lang='')
		where exists( select * from @temp as temp where temp.term_id=t.id and temp.entry_id=e.id )
end
--third word:
declare @word3 nvarchar(max); select top 3 @word3=word from @words
if(@word3<>@word2 and @word3<>@word1)
begin
	delete from @temp; insert into @temp(entry_id, term_id) select entry_id, term_id from @relateds; delete from @relateds
	delete from @tokens; insert into @tokens(token, lang) values(@word3, ''); insert into @tokens(token, lang) select token, lang from flex where lemma=@word3
	insert into @relateds(entry_id, lang, term_id) select distinct e.id, t.lang, t.id from entries as e
		inner join @temp as temp on temp.entry_id=e.id
		inner join entry_term as et on et.entry_id=e.id
		inner join terms as t on t.id=et.term_id
		inner join words as w on w.term_id=t.id
		inner join @tokens as tok on tok.token=w.word collate Latin1_General_CI_AS and (tok.lang=t.lang or tok.lang='')
		where exists( select * from @temp as temp where temp.term_id=t.id and temp.entry_id=e.id )
end

--return languages in which exact and/or related matches have been found:
select t.lang from (
	select entry_id, lang from @exacts union select entry_id, lang from @relateds
) as t
group by lang
order by count(*) desc

--return exact matches:
select distinct e.* from entries as e
	inner join @exacts as ids on ids.entry_id=e.id
	where @lang='' or @lang=ids.lang
	order by e.sortkeyGA

--return related matches:
select distinct top 101 e.* from entries as e
	inner join @relateds as ids on ids.entry_id=e.id
	where (@lang='' or @lang=ids.lang) and e.id not in (select entry_id from @exacts) and e.pStatus=1
	order by e.sortkeyGA

end
GO
