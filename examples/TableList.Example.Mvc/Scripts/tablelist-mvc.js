/*TableList.Mvc 1.0.7*/
$(function () {
    initTableList();


});

function tableListExternalBind() {
    if ($('.table-list-mvc tr td > input.date-picker')[0]) {
        $('.table-list-mvc tr td > input.date-picker').datetimepicker({
            format: 'MM/DD/YYYY'
        });
    }
}

function initTableList() {
    var form = $(".table-list-mvc").closest('form');

    if (form == 'undefined' || form.data("validator") == 'undefined') {
        return;
    }

    var ignore = form.data("validator").settings.ignore;
    if (ignore.indexOf(".table-list-mvc") == -1) {
        form.data("validator").settings.ignore = ".table-list-mvc-ignore, " + ignore;
    }

    $(form).submit(function (e) {
        if ($(this).valid()) {
            var trs = $(this).find(".table-list-mvc > tbody > tr.table-list-mvc-item-new:last");
            $(trs).each(function () {
                $(this).remove();
            });
        }
        return true;
    });

    tableListBind();
}

function tableListBind() {

    $('.table-list-mvc > tbody > tr').unbind('click').bind('click', function (e) {
        var tagName = e.target.tagName.toUpperCase();
        if (tagName != 'A') {
            var tr = $(e.target).closest('tr');
            $(tr).removeClass('table-list-mvc-item-view');

            if (tagName != 'INPUT' || e.target.readOnly) {
                var firstInput = $(tr).find('input:not([readonly]):first');
                firstInput.focus();
                var tmpStr = firstInput.val();
                firstInput.val('');
                firstInput.val(tmpStr);
            }
        }
    });

    $('.table-list-mvc tr:not(.table-list-mvc-item-new) td > input:not([readonly])').unbind('focusout').bind('focusout', function (e) {
        var tr = $(this).closest('tr');
        if (!tr.hasClass('table-list-mvc-item-view')) {
            tr.addClass('table-list-mvc-item-view');
        }
    });

    $('.table-list-mvc tr td > input').unbind('input').bind('input', function (e) {
        var tr = $(this).closest('tr');
        if (tr.hasClass('table-list-mvc-item-new') && !$(this).hasClass('date-picker')) {
            tableListCloneRow($(tr), $(this));
        }
    });

    $('.table-list-mvc tr td > input').unbind('change').bind('change', function (e, dp) {
        var tr = $(this).closest('tr');
        var state = tr.find("input[name$='TL_State']");
        if (state.val() == "Default") {
            state.val("Modified");
        }

        el = $(this);
        if (tr.hasClass('table-list-mvc-item-new') && $(this).hasClass('date-picker') && dp != 'undefined' && dp) {
            setTimeout(function () {
                tableListCloneRow(tr, el);
            }, 20);
        }

        if (tr.hasClass('table-list-mvc-item-new') && $(this).is(':checkbox')) {
            tableListCloneRow(tr, el);
        }
    });

    $('.table-list-mvc tr td > input.date-picker').unbind('dp.change').bind('dp.change', function (e) {
        var shown = $(this).attr('dp.shown');
        if (typeof shown != 'undefined' && parseInt(shown) && e.date != e.oldDate) {
            $(this).trigger('change', 1);
        }
    });

    $('.table-list-mvc tr td > input.date-picker').unbind('dp.show').bind('dp.show', function (e) {
        $(this).attr('dp.shown', 1);
    });

    $(".table-list-mvc input[data-group]").click(function () {
        var group = "input[data-group='" + $(this).attr("data-group") + "']";
        $(group).prop("checked", false);
        $(this).prop("checked", true);
    });

    $('.table-list-mvc-item-delete').unbind('click').bind('click', function (e) {
        e.preventDefault();
        var tr = $(this).closest('tr');
        var table = tr.closest('.table-list-mvc');
        var state = tr.find("input[name$='TL_State']");

        if (state.val() == "Added") {
            tr.remove();
            tableListRefresh(table);
            tableListBind();
        }
        else {
            state.val("Deleted");
            tr.hide();
        }
    });

    tableListExternalBind();
}

function tableListRefresh(el) {
    var trs = $(el).find('tbody tr');
    $(trs).each(function (newIndex) {
        tableListRefreshItem(this, newIndex);
    });
}

function tableListRefreshItem(el, newIndex) {
    var index = parseInt($(el).attr('data-item-index'));
    if (index == newIndex) {
        return;
    }

    $(el).attr('data-item-index', newIndex);

    var regexName = new RegExp("\\[" + index + "\\]", "g");
    var regexId = new RegExp("_" + index + "__", "g");

    var fields = $(el).find('[name]');
    var validateFields = $(el).find('[data-valmsg-for]');

    $(fields).each(function () {
        $(this).attr('name', $(this).attr('name').replace(regexName, '[' + newIndex + ']'));
        if (typeof $(this).attr('id') !== typeof undefined && $(this).attr('id') !== false) {
            $(this).attr('id', $(this).attr('id').replace(regexId, '_' + newIndex + '__'));
        }
    });

    $(validateFields).each(function () {
        $(this).attr('data-valmsg-for', $(this).attr('data-valmsg-for').replace(regexName, '[' + newIndex + ']'));
    });
}

function tableListCloneRow(tr, el) {
    var index = parseInt(tr.attr('data-item-index'));
    var newIndex = index + 1;

    var tempVal;

    if ($(el).is(':checkbox')) {
        tempVal = $(el).prop('checked');
        $(el).prop('checked', false);
    }
    else {
        tempVal = $(el).val();
        $(el).val('');
    }

    var shown = $(el).attr('dp.shown');
    if (typeof shown != 'undefined' && parseInt(shown)) {
        $(el).attr('dp.shown', 0);
    }

    var clone = tr.clone();
    clone.appendTo(tr.closest('.table-list-mvc').find('tbody'));

    tableListRefreshItem(clone, newIndex);

    tr.removeClass('table-list-mvc-item-new');
    tr.find(".table-list-mvc-ignore").removeClass("table-list-mvc-ignore");

    if ($(el).is(':checkbox')) {
        $(el).prop('checked', tempVal);
    }
    else {
        $(el).val(tempVal);
    }

    if (typeof shown != 'undefined' && parseInt(shown)) {
        $(el).attr('dp.shown', 1);
    }

    tableListValidate(el);
    tableListBind();
}

function tableListValidate(el) {

    var form = $(el).closest('form');

    if (form == 'undefined' || form.data("validator") == 'undefined') {
        return;
    }

    form.removeData("validator");
    form.removeData("unobtrusiveValidation");
    $.validator.unobtrusive.parse(form);

    var ignore = form.data("validator").settings.ignore;
    if (ignore.indexOf(".table-list-mvc") == -1) {
        form.data("validator").settings.ignore = ".table-list-mvc-ignore, " + ignore;
    }

    form.validate();
}