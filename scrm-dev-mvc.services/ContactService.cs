using Microsoft.AspNetCore.Mvc;
using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.Services;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;


namespace SCRM_dev.Services
{
    public class ContactService(IUnitOfWork unitOfWork) : IContactService
    {
        public async Task<string> CreateContactAsync(ContactDto contactDto)
        {
            if (contactDto == null)
            {
                return null;
            }
            if (string.IsNullOrEmpty(contactDto.Email))
            {
                return null;
            }
            var user = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == contactDto.OwnerId);

            if (user == null)
                return null;

            //check if contact already exists
            var existingContact = await unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Email == contactDto.Email && c.OrganizationId == user.OrganizationId);
            if (existingContact != null)
            {
                if (existingContact.IsDeleted == true)
                {
                    existingContact.IsDeleted = false;
                    existingContact.FirstName = contactDto.FirstName;
                    existingContact.LastName = contactDto.LastName;
                    existingContact.Number = contactDto.Number;
                    existingContact.Email = contactDto.Email;
                    existingContact.LeadStatusId = contactDto.LeadStatusId;
                    existingContact.LifeCycleStageId = contactDto.LifeCycleStageId;
                    existingContact.OwnerId = contactDto.OwnerId;
                    unitOfWork.Contacts.Update(existingContact);
                    await unitOfWork.SaveChangesAsync();
                    return "Contact created successfully";
                }
                else
                {
                    return "Contact with this email already exists";
                }

            }
            // Map ContactDto to Contact entity
            var contact = new Contact
            {
                FirstName = contactDto.FirstName,
                LastName = contactDto.LastName,
                Email = contactDto.Email,
                Number = contactDto.Number,
                JobTitle = contactDto.JobTitle,
                LeadStatusId = contactDto.LeadStatusId,
                LifeCycleStageId = contactDto.LifeCycleStageId,
                OrganizationId = user?.OrganizationId ?? 0,
                OwnerId = contactDto.OwnerId,
            };
            await unitOfWork.Contacts.AddAsync(contact);
            await unitOfWork.SaveChangesAsync();
            return "Contact created successfully";
        }

        public async Task<List<ContactResponseViewModel>> GetAllContacts(Guid userId)
        {
            var user = await unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return new List<ContactResponseViewModel>();

            Expression<Func<Contact, bool>> predicate;
            if (user.RoleId == 2 || user.RoleId == 3)
            {
                predicate = c => c.OrganizationId == user.OrganizationId && (!c.IsDeleted ?? false);
            }
            else
            {
                predicate = c => c.OwnerId == userId && c.OrganizationId == user.OrganizationId && (!c.IsDeleted ?? false);
            }

            // Eager load LeadStatus to avoid N+1 queries
            var contacts = await unitOfWork.Contacts.GetAllAsync(predicate, asNoTracking: true, c => c.LeadStatus);

            var contactResponseViewModels = contacts.Select(contact => new ContactResponseViewModel
            {
                Id = contact.Id,
                Name = $"{contact.FirstName} {contact.LastName}",
                Email = contact.Email,
                PhoneNumber = contact.Number,
                LeadStatus = contact.LeadStatus?.LeadStatusName ?? "N/A",
                CreatedAt = contact.CreatedAt,
            }).ToList();

            return contactResponseViewModels;
        }


        public async Task<IEnumerable<LeadStatus>> GetLeadStatusesAsync()
        {
            return await unitOfWork.LeadStatuses.GetAllAsync();
        }

        public async Task<IEnumerable<Lifecycle>> GetLifeCycleStagesAsync()
        {
            return await unitOfWork.Lifecycle.GetAllAsync();
        }

        public async Task<bool> DeleteContactsByIdsAsync(List<int> ids)
        {
            var contacts = await unitOfWork.Contacts.GetAllAsync(c => ids.Contains(c.Id));
            foreach (var contact in contacts)
            {
                contact.IsDeleted = true;
                unitOfWork.Contacts.Update(contact);
            }
            await unitOfWork.SaveChangesAsync();
            return true;

        }


        public async Task<string> UpdateContact(ContactDto contact)
        {
            if (contact != null)
            {
                Contact existingContact = await unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Id == contact.Id);
                if (existingContact != null)
                {
                    existingContact.FirstName = contact.FirstName;
                    existingContact.LastName = contact.LastName;
                    existingContact.Email = contact.Email;
                    existingContact.Number = contact.Number;
                    existingContact.JobTitle = contact.JobTitle;
                    existingContact.LeadStatusId = contact.LeadStatusId;
                    existingContact.LifeCycleStageId = contact.LifeCycleStageId;
                    existingContact.OwnerId = contact.OwnerId;
                    unitOfWork.Contacts.Update(existingContact);
                    await unitOfWork.SaveChangesAsync();
                    return "Contact updated successfully";
                }
            }
            return "Contact updation Failed";
        }

        public Contact GetContactById(int id)
        {
            var contact = unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Id == id, "LeadStatus,LifeCycleStage,Company,Deals,Activities.ActivityType").Result;
            return contact;
        }

        public async Task<(bool Success, string Message)> AssociateContactToCompany(int contactId, int companyId)
        {
            var contact = await unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Id == contactId);
            if (contact == null)
                return (false, "Contact not found.");

            // Optional: Check if company exists
            var companyExists = await unitOfWork.Company.AnyAsync(c => c.Id == companyId);
            if (!companyExists)
                return (false, "Company not found.");

            // Check if the contact is already associated
            if (contact.CompanyId != null)
            {
                if (contact.CompanyId == companyId)
                {
                    // This is a success, just no work was needed.
                    return (true, "Contact is already associated with this company.");
                }
                else
                {
                    // This is the specific error you wanted to propagate.
                    return (false, "Contact already belongs to another company.");
                }
            }

            // If we are here, contact.CompanyId is null, so we can associate.
            contact.CompanyId = companyId;
            unitOfWork.Contacts.Update(contact);

            try
            {
                await unitOfWork.SaveChangesAsync();
                return (true, "Contact associated successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error associating contact to company: {ex.Message}");
                return (false, "A database error occurred.");
            }
        }

        public async Task<bool> AssociateContactToDealAsync(int contactId, int dealId)
        {
            // 1. Fetch the contact and include its existing deals
            var contact = await unitOfWork.Contacts.FirstOrDefaultAsync(
                c => c.Id == contactId,
                include: "Deals" // Eager load the Deals collection
            );

            if (contact == null)
            {
                Debug.WriteLine($"AssociateContactToDeal: Contact with ID {contactId} not found.");
                return false;
            }

            // 2. Fetch the deal
            var deal = await unitOfWork.Deals.FirstOrDefaultAsync(d=> d.Id == dealId);
            if (deal == null)
            {
                Debug.WriteLine($"AssociateContactToDeal: Deal with ID {dealId} not found.");
                return false;
            }

            // 3. Check if the association already exists
            if (contact.Deals.Any(d => d.Id == dealId))
            {
                Debug.WriteLine($"AssociateContactToDeal: Contact {contactId} is already associated with Deal {dealId}.");
                return true; // Already associated, so it's "successful"
            }

            // 4. Create the association
            contact.Deals.Add(deal);
            unitOfWork.Contacts.Update(contact);

            // 5. Save
            try
            {
                await unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error associating contact to deal: {ex.Message}");
                return false;
            }
        }

        public async Task<(bool Success, string Message)> DisassociateCompany(int contactId)
        {
            var contact = await unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Id == contactId);

            // 1. Check if contact exists
            if (contact == null)
                return (false, "Contact not found.");

            // 2. Check if it's already disassociated
            if (contact.CompanyId == null)
                return (true, "Contact is already not associated with any company.");

            // 3. Perform the disassociation
            contact.CompanyId = null;
            unitOfWork.Contacts.Update(contact);

            try
            {
                await unitOfWork.SaveChangesAsync();
                return (true, "Company disassociated successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disassociating company: {ex.Message}");
                return (false, "A database error occurred.");
            }
        }
    }
}
