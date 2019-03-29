# CrudMatrixGenerator

Tool to read C# source code files and output  CRUD matrix.

It find DataContext and using Entity Framework and LINQ to SQL. 


## Usage

```
dotnet CrudMatrixGenerator.dll <Target Folder Path> <Output File Name> [-DesignerSourceCodeOnly]
```
- Target Folder Path : 
  Set path of a folder which includes C# source code files. 
  This should include declaration of DbContext of Entity Framework or DataContext of LINQ to SQL. 
  And this should also include files that use their Context.
- Output File Name : Set path of a file to output results text.
- -D or -DesignerSourceCodeOnly  (Optional) : 
  Set this option to find only \*.Context.cs or \*.designer.cs files as DbContext of Entity Framework or DataContext of LINQ to SQL.
  If omitted, all files are read.

### Examples

#### command

```
> dotnet CrudMatrixGenerator.dll c:\Source\Reps\App1 result.txt -D
```

#### console out
```
1 Context classes are found.
	App1Entities  : 1 properties

6 Using codes are found.
No	FileName	Line	Context	Property	Uses	CRUD
1	\App1\Main.cs	23	App1Entities 	Products	(LINQ)	R
2	\App1\Main.cs	56	App1Entities 	Products	(LINQ)	R U
3	\App1\Main.cs	59	App1Entities 	Products	Remove	D
4	\App1\Main.cs	65	App1Entities 	Products	Add	C
5	\App1\Main.cs	81	App1Entities 	Products	(LINQ)	R U
6	\App1\Main.cs	84	App1Entities 	Products	Remove	D


Output : result.txt

```

#### result.txt

| File | App1Entities.Products |
| :-- | :--: |
| \App1\Main.cs | C R U D |
