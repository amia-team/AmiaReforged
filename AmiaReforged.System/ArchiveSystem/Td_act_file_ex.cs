namespace AmiaReforged.System.ArchiveSystem;

public class Td_act_file_ex
{
    private const string VaultDir = "/nwn/home/servervault/";
    private const string ArchiveDir = "/archive/";
    private const int PageSize = 10;

    public Boolean MoveFile(string target, string destination, string fname)
    {
        if (!Directory.Exists(destination))
        {
            Directory.CreateDirectory(destination);
        }

        try
        {
            File.Move(target + fname, destination + fname);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public Boolean ArchiveFile(string cdkey, string fname)
    {
        string target = VaultDir + cdkey + "/";
        string destination = VaultDir + cdkey + ArchiveDir;
        return MoveFile(target, destination, fname);
    }

    public Boolean UnArchiveFile(string cdkey, string fname)
    {
        string target = VaultDir + cdkey + ArchiveDir;
        string destination = VaultDir + cdkey + "/";
        return MoveFile(target, destination, fname);
    }
    
    public Boolean RenameArchiveFile(string cdkey, string fname, string newname)
    {
        string target = VaultDir + cdkey + ArchiveDir + fname;
        string destination = VaultDir + cdkey + ArchiveDir + newname;
        try
        {
            File.Move(target, destination);
        }
        catch (Exception e)
        {
            return false;
        }

        return true;
    }

    public Boolean RenameVaultFile(string cdkey, string fname, string newname)
    {
        string target = VaultDir + cdkey + "/" + fname;
        string destination = VaultDir + cdkey + "/" + newname;
        try
        {
            File.Move(target, destination);
        }
        catch (Exception e)
        {
            return false;
        }

        return true;
    }

    public string[] GetVaultFiles(string cdkey)
    {
        string[] files = Directory.GetFiles(VaultDir + cdkey + "/");
        return files.Where(f => f.EndsWith(".bic")).ToArray();
    }

    public int GetVaultSize(string cdkey)
    {
        string[] files = Directory.GetFiles(VaultDir + cdkey + "/");
        return files.Length;
    }

    public string GetVaultFile(string cdkey, int index)
    {
        string[] files = Directory.GetFiles(VaultDir + cdkey + "/");
        string file = Path.GetFileName(files[index]);
        return file;
    }

    public string[] GetVaultPage(string cdkey, int page)
    {
        string[] fulllist = GetVaultFiles(cdkey);
        int size = fulllist.Length;
        int start = 10 * (page - 1);
        int end = (10 * page) - 1;
        if (end > size)
        {
            end = size;
        }

        string[] listpage = fulllist[start..end];
        return listpage;
    }

    public string[] GetArchiveFiles(string cdkey)
    {
        string[] files = Directory.GetFiles(VaultDir + cdkey + ArchiveDir);
        return files;
    }

    public int GetArchiveSize(string cdkey)
    {
        string[] files = Directory.GetFiles(VaultDir + cdkey + ArchiveDir);
        return files.Length;
    }

    public string GetArchiveFile(string cdkey, int index)
    {
        string[] files = Directory.GetFiles(VaultDir + cdkey + ArchiveDir);
        string file = Path.GetFileName(files[index]);
        return file;
    }

    public string[] GetArchivePage(string cdkey, int page)
    {
        string[] fulllist = GetArchiveFiles(cdkey);
        int size = fulllist.Length;
        int start = 10 * (page - 1);
        int end = (10 * page) - 1;
        if (end > size)
        {
            end = size;
        }

        string[] listpage = fulllist[start..end];
        return listpage;
    }

    public void SetArchiveConvPage(string cdkey, int page)
    {
        const int PAGE_TOKEN_VALUE = 10102;

        string[] files = GetArchivePage(cdkey, page);
        for (int loop = 0; loop < files.Length; loop++)
        {
        }
    }

    public Boolean RenameFile(string cdkey, int index, string newname, int isVault)
    {
        // TRUE
        if (isVault == 1)
        {
            string fname = GetVaultFile(cdkey, index);
            string target = VaultDir + cdkey + "/" + fname;
            string destination = VaultDir + cdkey + "/" + newname;
            try
            {
                File.Move(target, destination);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }
        else
        {
            string fname = GetArchiveFile(cdkey, index);
            string target = VaultDir + cdkey + ArchiveDir + fname;
            string destination = VaultDir + cdkey + ArchiveDir + newname;
            try
            {
                File.Move(target, destination);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }
    }
}