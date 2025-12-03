var dataTable;

$(document).ready(function () {
    loadDataTable();

    $('#tblData tbody').on('click', 'tr.contact-row td:not(:first-child)', function () {
        console.log('Row cell clicked!');
        var contactId = $(this).closest('tr').data('id');
        if (contactId) {
            window.location.href = '/Contact/ContactPreview/' + contactId;
        }
    });
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "serverSide": true,
        "processing": true, 

        "paging": true,

         scrollY: '280px', 
         scrollCollapse: true,

        "pageLength": 10, 

        "ajax": {
            "url": '/contact/getall',
            "type": "POST", 
            "contentType": "application/json",
            "data": function (d) {
                return JSON.stringify(d);
            }
        },

        "columns": [
            {
                data: null,
                orderable: false,
                render: function (data, type, row) {
                    return `<input type="checkbox" class="row-select" value="${row.id}"/>`;
                },
                "width": "1%"
            },
            { data: 'name', "width": "15%" },
            { data: 'email', "width": "15%" },
            { data: 'phoneNumber', "width": "15%" },
            { data: 'leadStatus', "width": "15%" },
            { data: 'ownerName', "width": "15%" },
            {
                data: 'createdAt',
                "width": "10%",
                render: function (data) {
                    return new Date(data).toLocaleDateString();
                }
            }
        ],
        "createdRow": function (row, data, dataIndex) {
            $(row).attr('data-id', data.id);
            $(row).addClass('contact-row');
        }
    });
}


$(document).on('change', '.row-select', function () {
    let selected = $('.row-select:checked').length;
    const actionBar = $('#actionBar');

    if (selected > 0) {
        actionBar.css('visibility', 'visible');
        actionBar.find('span').text(selected + ' selected');
    } else {
        actionBar.css('visibility', 'hidden');
    }
});

$('#btnDelete').click(function () {
    let ids = $('.row-select:checked').map(function () {
        return this.value;
    }).get();
    console.log(ids);


    if (ids.length > 0 && confirm('Delete selected contacts?')) {
        $.ajax({
            url: '/Contact/DeleteBulk',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(ids),
            success: function () {
                dataTable.ajax.reload();
                $('#actionBar').css('visibility', 'hidden');
            }
        });
    }
});


$('#btnEdit').click(function () {
    let ids = $('.row-select:checked').map(function () {
        return this.value;
    }).get();

    if (ids.length === 1) {
        window.location.href = `/Contact/Update/${ids[0]}`;
    } else if (ids.length > 1) {
        alert('Please select only one contact to edit.');
    } else {
        alert('Please select a contact to edit.');
    }
});


setTimeout(function () {
    var alert = document.getElementById('tempDataMessage');

    if (alert) {
        setTimeout(function () {
            alert.style.display = 'none';
        }, 5000); 
    }
}, 300);


