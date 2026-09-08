SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tblCache](
	[Word] [nvarchar](255) COLLATE Latin1_General_CI_AS NOT NULL,
	[Payload] [nvarchar](max) COLLATE Latin1_General_CI_AS NOT NULL,
	[When] [datetime] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[tblCache] ADD  CONSTRAINT [DF_tblCache_When]  DEFAULT (getdate()) FOR [When]
GO
