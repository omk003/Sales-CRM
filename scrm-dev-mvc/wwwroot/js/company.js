var dataTable;

$(document).ready(function () {
    loadDataTable();
    // This goes after your DataTable setup
    $('#tblData tbody').on('click', 'tr.company-row td:not(:first-child)', function () {
        console.log('Row cell clicked!');
        var companyId = $(this).closest('tr').data('id');
        if (companyId) {
            window.location.href = '/Company/CompanyPreview/' + companyId;
        }
    });
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        scrollY: '280px',
        scrollCollapse: true,
        paging: false,
        "ajax": { url: '/company/getall' },
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
            { data: 'city', "width": "15%" },
            { data: 'country', "width": "15%" },
            { data: 'userName', "width": "15%" },
            { data: 'createdDate', "width": "10%" }
        ],
        // Highlight: Use createdRow to assign data-id
        "createdRow": function (row, data, dataIndex) {
            $(row).attr('data-id', data.id);
            $(row).addClass('company-row');
        }
    });
}
$(document).on('change', '.row-select', function () {
    let selected = $('.row-select:checked').length;
    const actionBar = $('#actionBar');

    if (selected > 0) {
        // show the buttons inside actionBar
        actionBar.css('visibility', 'visible');
        actionBar.find('span').text(selected + ' selected');
    } else {
        // hide buttons but keep the container space
        actionBar.css('visibility', 'hidden');
    }
});

$('#btnDelete').click(function () {
    let ids = $('.row-select:checked').map(function () {
        return this.value;
    }).get();
    console.log(ids);


    if (ids.length > 0 && confirm('Delete selected companies?')) {
        $.ajax({
            url: '/Company/DeleteBulk',
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
        window.location.href = `/Company/Update/${ids[0]}`;
    } else if (ids.length > 1) {
        alert('Please select only one company to edit.');
    } else {
        alert('Please select a company to edit.');
    }
});


setTimeout(function () {
    // Find the alert message element by its ID
    var alert = document.getElementById('tempDataMessage');

    // If the element exists, set another timeout to hide it
    if (alert) {
        setTimeout(function () {
            // Hides the element by setting its display style to 'none'
            alert.style.display = 'none';
        }, 5000); // 5000 milliseconds = 5 seconds
    }
}, 300);