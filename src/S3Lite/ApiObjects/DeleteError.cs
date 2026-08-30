namespace S3Lite.ApiObjects
{
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// Describes a single object that could not be deleted as part of a delete-multiple
    /// (batch delete) operation.  The delete-multiple call returns HTTP 200 even when some
    /// objects fail; per-object failures are surfaced here rather than as a thrown exception.
    /// </summary>
    [XmlRoot(ElementName = "Error", IsNullable = true)]
    public class DeleteError
    {
        // Namespace = "http://s3.amazonaws.com/doc/2006-03-01/"

        #region Public-Members

        /// <summary>
        /// Object key that failed to delete.
        /// </summary>
        [XmlElement(ElementName = "Key", IsNullable = true)]
        public string Key { get; set; } = null;

        /// <summary>
        /// Version ID associated with the failed delete, when applicable.
        /// </summary>
        [XmlElement(ElementName = "VersionId", IsNullable = true)]
        public string VersionId { get; set; } = null;

        /// <summary>
        /// S3 error code string, for example <c>AccessDenied</c> or <c>NoSuchKey</c>.
        /// </summary>
        [XmlElement(ElementName = "Code", IsNullable = true)]
        public string Code { get; set; } = null;

        /// <summary>
        /// Human-readable error message describing the failure.
        /// </summary>
        [XmlElement(ElementName = "Message", IsNullable = true)]
        public string Message { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public DeleteError()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="key">Object key.</param>
        /// <param name="code">Error code.</param>
        /// <param name="message">Error message.</param>
        /// <param name="versionId">Version ID.</param>
        public DeleteError(string key, string code, string message, string versionId = null)
        {
            Key = key;
            Code = code;
            Message = message;
            VersionId = versionId;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Helper method for XML serialization.
        /// </summary>
        /// <returns>Boolean.</returns>
        public bool ShouldSerializeVersionId()
        {
            return !String.IsNullOrEmpty(VersionId);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
