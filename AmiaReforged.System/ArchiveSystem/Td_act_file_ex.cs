namespace AmiaReforged.System.ArchiveSystem;

public class Td_act_file_ex
{
    private const string VAULT_DIR = "/nwn/home/servervault/";
    private const string ARCHIVE_DIR = "/archive/";
    private const int PAGE_SIZE = 10;
    public Boolean MoveFile(string target, string destination, string fname)
    {
        if (!global::System.IO.Directory.Exists(destination)) {
            global::System.IO.Directory.CreateDirectory(destination);
        }
        try {
            global::System.IO.File.Move(target + fname, destination + fname);
        } catch (Exception e) {
            return false;
        }
        return true;
    }
    public Boolean ArchiveFile(string cdkey, string fname) {
        string target = VAULT_DIR + cdkey + "/";
        string destination = VAULT_DIR + cdkey + ARCHIVE_DIR;
        return MoveFile(target, destination, fname);
    }
    public Boolean ArchiveFile(string cdkey, int index) {
        string fname = GetVaultFile(cdkey, index);
        return ArchiveFile(cdkey, fname);
    }
    public Boolean UnArchiveFile(string cdkey, string fname)
    {
        string target = VAULT_DIR + cdkey + ARCHIVE_DIR;
        string destination = VAULT_DIR + cdkey + "/";
        return MoveFile(target, destination, fname);
    }
    public Boolean UnArchiveFile(string cdkey, int index){
        string fname = GetArchiveFile(cdkey, index);
        return ArchiveFile(cdkey, fname);
    }
    public Boolean RenameArchiveFile(string cdkey, string fname, string newname) {
        string target = VAULT_DIR + cdkey + ARCHIVE_DIR + fname;
        string destination = VAULT_DIR + cdkey + ARCHIVE_DIR + newname;
        try {
            global::System.IO.File.Move(target, destination);
        } catch (Exception e) {
            return false;
        }
        return true;
    }
    public Boolean RenameVaultFile(string cdkey, string fname, string newname) {
        string target = VAULT_DIR + cdkey + "/" + fname;
        string destination = VAULT_DIR + cdkey + "/" + newname;
        try {
            global::System.IO.File.Move(target, destination);
        } catch (Exception e) {
            return false;
        }
        return true;
    }
    public string[] GetVaultFiles(string cdkey) {
        string[] files = global::System.IO.Directory.GetFiles(VAULT_DIR + cdkey + "/");
        return files;
    }
    public int GetVaultSize(string cdkey) {
        string[] files = global::System.IO.Directory.GetFiles(VAULT_DIR + cdkey + "/");
        return files.Length;
    }
    public string GetVaultFile(string cdkey, int index) {
        string[] files = global::System.IO.Directory.GetFiles(VAULT_DIR + cdkey + "/");
        string file = Path.GetFileName(files[index]);
        return file;
    }

    public string[] GetVaultPage(string cdkey, int page) {
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
    public string[] GetArchiveFiles(string cdkey) {
        string[] files = global::System.IO.Directory.GetFiles(VAULT_DIR + cdkey + ARCHIVE_DIR);
        return files;
    }
    public int GetArchiveSize(string cdkey) { 
        string[] files = global::System.IO.Directory.GetFiles(VAULT_DIR + cdkey + ARCHIVE_DIR);
        return files.Length;
    }
    public string GetArchiveFile(string cdkey, int index) {
        string[] files = global::System.IO.Directory.GetFiles(VAULT_DIR + cdkey + ARCHIVE_DIR);
        string file = Path.GetFileName(files[index]);
        return file;
    }
    public string[] GetArchivePage(string cdkey, int page) {
        string[] fulllist = GetArchiveFiles(cdkey);
        int size = fulllist.Length;
        int start = 10 * (page - 1);
        int end = (10 * page) - 1;
        if (end > size) {
            end = size; 
        }
        string[] listpage = fulllist[start..end];
        return listpage;
    }

    public void SetArchiveConvPage(string cdkey, int page) {
        const int PAGE_TOKEN_VALUE = 10102;

        string[] files = GetArchivePage(cdkey, page);
        for (int loop = 0; loop < files.Length;loop++) {

        }
    }

    public Boolean RenameFile(string cdkey, int index, string newname, int isVault) {
        // TRUE
        if (isVault == 1) {
            string fname = GetVaultFile(cdkey, index);
            string target = VAULT_DIR + cdkey + "/" + fname;
            string destination = VAULT_DIR + cdkey + "/" + newname;
            try {
                global::System.IO.File.Move(target, destination);
            } catch (Exception e) {
                return false;
            }
            return true;
        } else {
            string fname = GetArchiveFile(cdkey, index);
            string target = VAULT_DIR + cdkey + ARCHIVE_DIR + fname;
            string destination = VAULT_DIR + cdkey + ARCHIVE_DIR + newname;
            try
            {
                global::System.IO.File.Move(target, destination);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }
    }
}
