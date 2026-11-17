using System.Collections.Generic;

public interface IImportService
{
	List<SpendingItem> Import(string path, int year, int month);

}
