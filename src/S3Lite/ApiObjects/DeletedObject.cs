namespace S3Lite.ApiObjects
{
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// Describes a single object that was successfully deleted as part of a delete-multiple
    /// (batch delete) operation.
    /// </summary>
    [XmlRoot(ElementName = "Deleted", IsNullable = true)]
    public class DeletedObject
    {
        // Namespace = "http://s3.amazonaws.com/doc/2006-03-01/"

        #region Public-Members

        /// <summary>
        /// Object key.
        /// </summary>
        [XmlElement(ElementName = "Key", IsNullable = true)]
        public string Key { get; set; } = null;

        /// <summary>
        /// Version ID of the deleted object, when versioning applies.
        /// </summary>
        [XmlElement(ElementName = "VersionId", IsNullable = true)]
        public string VersionId { get; set; } = null;

        /// <summary>
        /// Indicates whether the deleted object was (or became) a delete marker.
        /// Null when the server did not report this field.
        /// </summary>
        [XmlElement(ElementName = "DeleteMarker", IsNullable = true)]
        public bool? DeleteMarker { get; set; } = null;

        /// <summary>
        /// Version ID of the delete marker created by the delete, when applicable.
        /// </summary>
        [XmlElement(ElementName = "DeleteMarkerVersionId", IsNullable = true)]
        public string DeleteMarkerVersionId { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public DeletedObject()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="key">Object key.</param>
        /// <param name="versionId">Version ID.</param>
        public DeletedObject(string key, string versionId = null)
        {
            Key = key;
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

        /// <summary>
        /// Helper method for XML serialization.
        /// </summary>
        /// <returns>Boolean.</returns>
        public bool ShouldSerializeDeleteMarker()
        {
            return DeleteMarker.HasValue;
        }

        /// <summary>
        /// Helper method for XML serialization.
        /// </summary>
        /// <returns>Boolean.</returns>
        public bool ShouldSerializeDeleteMarkerVersionId()
        {
            return !String.IsNullOrEmpty(DeleteMarkerVersionId);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
