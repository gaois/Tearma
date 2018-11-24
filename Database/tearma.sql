/****** Object:  Table [dbo].[comments]    Script Date: 24/11/2018 21:09:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[comments](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[entry_id] [int] NULL,
	[extranet_id] [int] NULL,
	[tag_id] [int] NULL,
	[when] [datetime] NULL,
	[email] [nvarchar](max) NULL,
	[body] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[configs]    Script Date: 24/11/2018 21:09:15 ******/
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
/****** Object:  Table [dbo].[entries]    Script Date: 24/11/2018 21:09:15 ******/
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
 CONSTRAINT [PK__entries__3213E83FB84A5A39] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[entry_collection]    Script Date: 24/11/2018 21:09:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[entry_collection](
	[entry_id] [int] NULL,
	[collection] [int] NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[entry_domain]    Script Date: 24/11/2018 21:09:15 ******/
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
/****** Object:  Table [dbo].[entry_extranet]    Script Date: 24/11/2018 21:09:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[entry_extranet](
	[entry_id] [int] NULL,
	[extranet] [int] NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[entry_sortkey]    Script Date: 24/11/2018 21:09:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[entry_sortkey](
	[entry_id] [int] NULL,
	[lang] [nvarchar](max) NULL,
	[key] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[entry_term]    Script Date: 24/11/2018 21:09:15 ******/
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
/****** Object:  Table [dbo].[history]    Script Date: 24/11/2018 21:09:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[history](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[entry_id] [int] NULL,
	[action] [nvarchar](max) NULL,
	[when] [datetime] NULL,
	[email] [nvarchar](max) NULL,
	[json] [nvarchar](max) NULL,
	[historiography] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[metadata]    Script Date: 24/11/2018 21:09:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[metadata](
	[id] [int] NOT NULL,
	[type] [varchar](255) NULL,
	[json] [nvarchar](max) NULL,
	[sortkey] [nvarchar](max) NULL,
 CONSTRAINT [PK__metadata__3213E83FD56CDBC2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[terms]    Script Date: 24/11/2018 21:09:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[terms](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[json] [nvarchar](max) NULL,
	[lang] [char](10) NULL,
	[wording] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[words]    Script Date: 24/11/2018 21:09:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[words](
	[term_id] [int] NULL,
	[word] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  StoredProcedure [dbo].[pub_advsearch]    Script Date: 24/11/2018 21:09:15 ******/
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
select * from metadata where type in ('acceptLabel', 'inflectLabel', 'posLabel', 'domain') order by sortkey

--return matches:
select top 20 * from entries order by newid()

--return pager:
select @page as [currentPage], 12 as [maxPage]

end
GO
/****** Object:  StoredProcedure [dbo].[pub_advsearch_prepare]    Script Date: 24/11/2018 21:09:15 ******/
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
/****** Object:  StoredProcedure [dbo].[pub_domain]    Script Date: 24/11/2018 21:09:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create procedure [dbo].[pub_domain]
  @lang nvarchar(255) --empty string means any language
, @domID int
, @subdomID int --zero means any
, @page int = 1
as
begin

--return lingo and metadata:
select * from configs where id='lingo'
select * from metadata where type in ('acceptLabel', 'inflectLabel', 'posLabel', 'domain') order by sortkey

--return matches:
select top 20 * from entries order by newid()

--return pager:
select @page as [currentPage], 12 as [maxPage]

end
GO
/****** Object:  StoredProcedure [dbo].[pub_domains]    Script Date: 24/11/2018 21:09:15 ******/
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
select * from metadata where [type]='domain' order by sortkey

end
GO
/****** Object:  StoredProcedure [dbo].[pub_index]    Script Date: 24/11/2018 21:09:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create procedure [dbo].[pub_index]
as
begin

--return lingo and metadata:
select * from configs where id='lingo'
select * from metadata where type in ('acceptLabel', 'inflectLabel', 'posLabel', 'domain') order by sortkey

end
GO
/****** Object:  StoredProcedure [dbo].[pub_quicksearch]    Script Date: 24/11/2018 21:09:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[pub_quicksearch]
  @word nvarchar(255)
, @lang nvarchar(255) --empty string means any language
as
begin

--return lingo and metadata:
select * from configs where id='lingo'
select * from metadata where type in ('acceptLabel', 'inflectLabel', 'posLabel', 'domain') order by sortkey

--return similars:
select 'frgttgtrr' as similar
union select 'cdsgetrgtgtrhr' as similar
union select 'xsfrgt5yh6h4t' as similar

--return exact matches:
select top 2 * from entries order by newid()

--return related matches:
select top 20 * from entries order by newid()

--say whether there are any more related matches:
select convert(bit, 1) as relatedMore

--return languages in which exact and/or related matches have been found:
select 'ga' as lang
union select 'en' as lang
union select 'de' as lang

end
GO
