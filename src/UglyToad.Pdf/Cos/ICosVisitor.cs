namespace UglyToad.Pdf.Cos
{
    /**
     * An interface for visiting a PDF document at the type (COS) level.
     *
     * @author Michael Traut
     */
    public interface ICosVisitor
    {
        /**
         * Notification of visit to Array object.
         *
         * @param obj The Object that is being visited.
         * @return any Object depending on the visitor implementation, or null
         * @throws IOException If there is an error while visiting this object.
         */
        object VisitFromArray(COSArray obj);

        /**
         * Notification of visit to boolean object.
         *
         * @param obj The Object that is being visited.
         * @return any Object depending on the visitor implementation, or null
         * @throws IOException If there is an error while visiting this object.
         */
        object VisitFromBoolean(CosBoolean obj);

        /**
         * Notification of visit to dictionary object.
         *
         * @param obj The Object that is being visited.
         * @return any Object depending on the visitor implementation, or null
         * @throws IOException If there is an error while visiting this object.
         */
        object VisitFromDictionary(CosDictionary obj);

        /**
         * Notification of visit to document object.
         *
         * @param obj The Object that is being visited.
         * @return any Object depending on the visitor implementation, or null
         * @throws IOException If there is an error while visiting this object.
         */
         object VisitFromDocument(COSDocument obj);

        /**
         * Notification of visit to float object.
         *
         * @param obj The Object that is being visited.
         * @return any Object depending on the visitor implementation, or null
         * @throws IOException If there is an error while visiting this object.
         */
        object VisitFromFloat(CosFloat obj);

        /**
         * Notification of visit to integer object.
         *
         * @param obj The Object that is being visited.
         * @return any Object depending on the visitor implementation, or null
         * @throws IOException If there is an error while visiting this object.
         */
        object VisitFromInt(CosInt obj);

        /**
         * Notification of visit to name object.
         *
         * @param obj The Object that is being visited.
         * @return any Object depending on the visitor implementation, or null
         * @throws IOException If there is an error while visiting this object.
         */
        object VisitFromName(CosName obj);

        /**
         * Notification of visit to null object.
         *
         * @param obj The Object that is being visited.
         * @return any Object depending on the visitor implementation, or null
         * @throws IOException If there is an error while visiting this object.
         */
        object VisitFromNull(CosNull obj);

        /**
         * Notification of visit to stream object.
         *
         * @param obj The Object that is being visited.
         * @return any Object depending on the visitor implementation, or null
         * @throws IOException If there is an error while visiting this object.
         */
        object VisitFromStream(COSStream obj);

        /**
         * Notification of visit to string object.
         *
         * @param obj The Object that is being visited.
         * @return any Object depending on the visitor implementation, or null
         * @throws IOException If there is an error while visiting this object.
         */
        object VisitFromString(CosString obj);
    }
}
