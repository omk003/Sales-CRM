namespace scrm_dev_mvc.services
{
    public static class WorkflowServiceHelpers
    {
        public static bool TryGetContactId(object entity, out int contactId)
        {
            contactId = 0;
            if (entity is Models.Contact contact)
            {
                contactId = contact.Id;
                return true;
            }
            if (entity is Models.Task task && task.ContactId.HasValue)
            {
                contactId = task.ContactId.Value;
                return true;
            }
            return false;
        }
    }
}