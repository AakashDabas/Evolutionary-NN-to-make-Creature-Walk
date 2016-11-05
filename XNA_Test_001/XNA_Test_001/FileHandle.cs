using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace FileHandle
{
    class FileHandler
    {
        #region Declarations

        Stream stream = null;
        BinaryFormatter bFormatter = null;
        string fileName;

        #endregion

        public FileHandler(string fileName)
        {
            this.fileName = fileName;
            bFormatter = new BinaryFormatter();
        }

        public void Write(object obj)
        {
            stream = File.Open(fileName, FileMode.Append);
            bFormatter.Serialize(stream, obj);
            stream.Close();
        }

        public object Read()
        {
            object obj = null;
            try
            {
                stream = File.Open(fileName, FileMode.Open);
                if (stream.CanSeek)
                    obj = bFormatter.Deserialize(stream);
                stream.Close();
            }
            catch
            {
                System.Console.WriteLine("Enter Valid File Name!!!");
            }
            return obj;
        }
    }
}
