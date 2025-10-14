using Microsoft.AspNetCore.Mvc;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.Services;
using System.Linq;

namespace SCRM_dev.Services
{
    public class ContactService(IUnitOfWork unitOfWork) : IContactService
    {
        public async Task<string> CreateContactAsync(ContactDto contactDto)
        {
            if(contactDto == null)
            {
                return null;
            }
            if (string.IsNullOrEmpty(contactDto.Email))
            {
                return null;
            }
            var user = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == contactDto.OwnerId);

            //check if contact already exists
            var existingContact = await unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Email == contactDto.Email && c.OrganizationId == user.OrganizationId);
            if(existingContact != null)
            {
                if(existingContact.IsDeleted == true)
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
            // user not found
            if (user == null)
                return new List<ContactResponseViewModel>();

            // user is admin
            if (user.RoleId == 2 || user.RoleId ==3)
            {
                var organizationId = user.OrganizationId;
                var ContactList = await unitOfWork.Contacts.GetAllAsync(c => c.OrganizationId == organizationId && c.IsDeleted == false);


                List<ContactResponseViewModel> contactResponseViewModels = new List<ContactResponseViewModel>();
                foreach (var contact in ContactList)
                {
                    var id = contact.Id;
                    var contactWithLeadStatus = await unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Id == id, "LeadStatus");
                    var leadStatus = contactWithLeadStatus?.LeadStatus;
                    contactResponseViewModels.Add(new ContactResponseViewModel
                    {
                        Id = id,

                        Name = contact.FirstName + " " + contact.LastName,
                        Email = contact.Email,
                        PhoneNumber = contact.Number,
                        LeadStatus = leadStatus != null ? leadStatus.LeadStatusName : "N/A",
                        CreatedAt = contact.CreatedAt,
                    });
                }
                return contactResponseViewModels;

            }
            // user is regular user
            var ContactObject = await unitOfWork.Contacts.GetAllAsync(c => c.OwnerId == userId && c.OrganizationId == user.OrganizationId && (c.IsDeleted == false));

            List<ContactResponseViewModel> contactResponseViewModelsObject = new List<ContactResponseViewModel>();
            foreach (var contact in ContactObject)
            {
                var id = contact.Id;
                var contactWithLeadStatus = await unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Id == id, "LeadStatus");
                var leadStatus = contactWithLeadStatus?.LeadStatus;
                contactResponseViewModelsObject.Add(new ContactResponseViewModel
                {
                    Id = id,

                    Name = contact.FirstName + " " + contact.LastName,
                    Email = contact.Email,
                    PhoneNumber = contact.Number,
                    LeadStatus = leadStatus != null ? leadStatus.LeadStatusName : "N/A",
                    CreatedAt = contact.CreatedAt,
                });
            }
            return contactResponseViewModelsObject;

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
           foreach(var id in ids)
            {
                var contact = await unitOfWork.Contacts.FirstOrDefaultAsync(u => u.Id == id);
                if(contact != null)
                {
                    contact.IsDeleted = true;
                    unitOfWork.Contacts.Update(contact);
                }
            }
            await unitOfWork.SaveChangesAsync();
            return true;

        }


        public async Task<string> UpdateContact(ContactDto contact)
        {
            if (contact != null)
            {
                Contact existingContact = unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Id == contact.Id).Result;
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
            var contact = unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Id == id).Result;
            return contact;
        }

    }
}
