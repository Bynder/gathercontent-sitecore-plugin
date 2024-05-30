namespace Bynder.Content.SitecoreConnector.Core.Interfaces
{
    using Models.Import;

    public interface IMediaRepository<T>: IRepository where T: class 
    {
        T UploadFile(string targetPath, File fileInfo);

        string ResolveMediaPath(CmsItem item, T createdItem, CmsField field);
    }
}
