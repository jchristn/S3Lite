namespace S3Lite.ApiObjects
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// Result of a delete-multiple (batch delete) operation.
    /// Contains the objects that were deleted and, separately, any per-object failures.
    /// </summary>
    [XmlRoot(ElementName = "DeleteResult", IsNullable = true)]
    public class DeleteResult
    {
        // Namespace = "http://s3.amazonaws.com/doc/2006-03-01/"

        #region Public-Members

        /// <summary>
        /// Objects that were successfully deleted.
        /// Empty when the request used quiet mode.
        /// </summary>
        [XmlElement(ElementName = "Deleted", IsNullable = true)]
        public List<DeletedObject> Deleted
        {
            get
            {
                return _Deleted;
            }
            set
            {
                _Deleted = value ?? new List<DeletedObject>();
            }
        }

        /// <summary>
        /// Objects that could not be deleted, each with its error code and message.
        /// </summary>
        [XmlElement(ElementName = "Error", IsNullable = true)]
        public List<DeleteError> Errors
        {
            get
            {
                return _Errors;
            }
            set
            {
                _Errors = value ?? new List<DeleteError>();
            }
        }

        #endregion

        #region Private-Members

        private List<DeletedObject> _Deleted = new List<DeletedObject>();
        private List<DeleteError> _Errors = new List<DeleteError>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public DeleteResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
