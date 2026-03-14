jQuery(function ($) {
    buttonActionchange = function (actionselection) {
        
        var isredirectselect = false;
        var isemailselect = false;
        var opts = [],
            opt;
        var len = actionselection.options.length;
        
        for (var i = 0; i < len; i++) {
            opt = actionselection.options[i];
            console.log(opt.selected); // opt.selected
            if (opt.selected) {
                opts.push(opt);
                //console.log(opt.value);
                if (opt.value == 'redirect') {
                    isredirectselect = true;
                    //console.log(actionselection);
                }
                if (opt.value == 'sendemail') {
                    isemailselect = true;
                    //console.log(actionselection);
                }
            }
            if (isredirectselect) {
                $(".ActionRedirect-wrap").show();

            }
            else {
                $(".ActionRedirect-wrap").hide();

            }
            if(isemailselect) { 
                $(".actionSendEmail-wrap").show();
            }
            else {
                $(".actionSendEmail-wrap").hide();
            }
        }
    }
    var formhasvalue = false;
    GetjsonDataFromData = function (WebFormFieldsData) {
        var resultData = [];
        if (WebFormFieldsData != null && WebFormFieldsData != undefined) {
            if (WebFormFieldsData != null && WebFormFieldsData.length > 0) {
                var FieldsData = WebFormFieldsData.sort(function (a, b) { return a.Sequence - b.Sequence });

                $.each(WebFormFieldsData, function (index, data) {
                    var NewData = {};
                    NewData.type = data.type;
                    NewData.label = data.label;
                    NewData.subtype = data.subtype;
                    NewData.className = data.className;
                    NewData.name = data.name;
                    NewData.description = data.description;
                    NewData.placeholder = data.placeholder;
                    NewData.value = data.value;
                    NewData.style = data.style;
                    NewData.required = data.required;
                    NewData.min = data.min;
                    NewData.max = data.max;
                    NewData.maxlength = data.maxlength;
                    NewData.Rows = data.rows;
                    NewData.multiple = data.multiple;
                    NewData.toggle = data.toggle;
                    NewData.inline = data.inline;
                    NewData.other = data.other;
                    if (data.action != null) {
                        var btnselection = data.action.split(',');
                        NewData.action = [];
                        for (var a in btnselection) {
                            NewData.action.push(btnselection[a]);
                            if (btnselection[a] == "redirect") {
                                NewData.ActionRedirect = data.actionRedirect;
                            }
                            if (btnselection[a] == "sendemail") {
                                NewData.actionSendEmail = data.actionSendemail;

                            }
                        }
                    }
                   
                    NewData.arroWebFormsId = data.arroWebFormsId;
                    NewData.values = [];
                    
                    if (data.values != null && data.values.length > 0) {
                        var FieldsValueData = data.values.sort(function (a, b) { return a.Sequence - b.Sequence });
                        $.each(FieldsValueData, function (vIndex, vData) {
                            var newvalues = {};
                            newvalues.label = vData.label;
                            newvalues.value = vData.value;
                            newvalues.selected = vData.selected;
                            newvalues.Id = vData.id;
                            NewData.values.push(newvalues);
                        });
                    }

                    resultData.push(NewData);

                });

            }
        }

        return resultData;
    }
    var formDefaultData = "";
    if (formdata != undefined && formdata != null && formdata.WebFormFields != undefined && formdata.WebFormFields != null) {
        formDefaultData = JSON.stringify(GetjsonDataFromData(formdata.WebFormFields));
    }
    var replaceandcount = formDefaultData.replace("[", "").replace("]", "");
    if (replaceandcount.length > 0)
        formhasvalue = true;


    var options = {
        
        formData: formDefaultData,
        typeUserDisabledAttrs: {
            'button': [
              'style',
            ]
          },
        typeUserAttrs: {
            header: {
                WebFormFieldsId: {
                    value: '',
                    type: 'hidden'
                }
            },
            paragraph: {
                WebFormFieldsId: {
                    value: '',
                    type: 'hidden'
                }
            },
            text: {
                WebFormFieldsId: {
                    value: '',
                    type: 'hidden'
                }
            },
            autocomplete: {
                WebFormFieldsId: {
                    value: '',
                    type: 'hidden'
                }
            },
            textarea: {
                WebFormFieldsId: {
                    value: '',
                    type: 'hidden'
                }
            },
            number: {
                WebFormFieldsId: {
                    value: '',
                    type: 'hidden'
                }
            },
            date: {
                WebFormFieldsId: {
                    value: '',
                    type: 'hidden'
                }
            },
            hidden: {
                WebFormFieldsId: {
                    value: '',
                    type: 'hidden'
                }
            },
            select: {
                WebFormFieldsId: {
                    value: '',
                    type: 'hidden'
                }
            },
            "checkbox-group": {
                WebFormFieldsId: {
                    value: '',
                    type: 'hidden'
                }
            },
            checkbox: {
                WebFormFieldsId: {
                    value: '',
                    type: 'hidden'
                }
            },
            "radio-group": {
                WebFormFieldsId: {
                    value: '',
                    type: 'hidden'
                }
            },
            file: {
                WebFormFieldsId: {
                    value: '',
                    type: 'hidden'
                }
            },
            button: {
                action: {
                    label: 'actions',
                    multiple: true,
                    type: 'checkbox-group',
                    options: {
                        'sendemail': 'Send Email',
                        'redirect': 'Redirect To Page',
                    },
                    onchange: 'buttonActionchange(this)'
                },
                actionSendEmail: {
                    label: 'Email Address',
                    type: 'email',
                    value: '',
                    placeholder: 'Enter Email Address',
                },
                
                ActionRedirect: {
                    label: 'Redirect Url',
                    type: 'text',
                    value: '',
                    placeholder: 'Enter Redirect Url',

                },
                WebFormFieldsId: {
                    value: '',
                    type: 'hidden'
                },
                


            }
        },
        controlOrder: ["header",
            "paragraph",
            "text",
            "textarea",
            "number",
            "date",
            "autocomplete",
            "select",
            "checkbox",
            "checkbox-group",
            "radio-group",
            "file",
            "button"],
        disabledAttrs: ["access", "step", "toggle", "multiple"],
        disabledActionButtons: ['data'],
        disabledSubtypes: {
            textarea: ['tinymce', 'quill'],
            file: ['fineuploader'],
        },
        dataType: 'json',
        fieldRemoveWarn: true,
        onSave: function (tevt, formData) {

            getformdata();

        },
        typeUserEvents: {
            "autocomplete": {
                onadd: function (fld) {
                    var currentoption = $('[data-attr="Id"]');
                    if (currentoption.length > 0) {
                        $(currentoption).hide();
                    }
                }
            },
            "select": {
                onadd: function (fld) {

                    var currentoption = $('[data-attr="Id"]');
                    if (currentoption.length > 0) {
                        $(currentoption).hide();
                    }
                }
            },
            "checkbox-group": {
                onadd: function (fld) {
                    var currentoption = $('[data-attr="Id"]');
                    if (currentoption.length > 0) {
                        $(currentoption).hide();
                    }
                }
            },
            "checkbox": {
                onadd: function (fld) {
                    var currentoption = $('[data-attr="Id"]');
                    if (currentoption.length > 0) {
                        $(currentoption).hide();
                    }
                }
            },
            "radio-group": {
                onadd: function (fld) {
                    var currentoption = $('[data-attr="Id"]');
                    if (currentoption.length > 0) {
                        $(currentoption).hide();
                    }
                }
            },
            "button": {
                onadd: function (fld) {
                    var redirectSelected = false;
                    var sendemailSelected = false;
                    $(fld).find("[name='action']").find(":selected").each(function () {
                        if ($(this).val() === 'redirect')
                            redirectSelected = true;
                        if ($(this).val() === 'sendemail')
                            sendemailSelected = true;
                    });
                    var currentoption = $('.ActionRedirect-wrap');
                    if (currentoption.length > 0 && !redirectSelected) {
                        $(currentoption).hide();
                    }
                    var currentoption = $('.actionSendEmail-wrap');
                    if (currentoption.length > 0 && !sendemailSelected) {
                        $(currentoption).hide();
                    }
                }
            },
        },
    };
    var formbuilderDev = $(document.getElementById('fb-editor')).formBuilder(options);
    var formdata;
    getformdata = function () {
        //  console.log(formhasvalue)
        var fieldslist = formbuilderDev.actions.getData();
        if (formhasvalue && fieldslist.length <= 0) {
            fieldslist = "null";
        }
        $.each(fieldslist, function (index, data) {
            if (data.type === 'button' && typeof (data.action) !== "undefined" && data.action !== null) {
                var changearrytostring = "";
                $.each(data.action, function (indexA, DataA) {
                    if (indexA > 0) {
                        changearrytostring += ",";
                    }
                    changearrytostring += DataA;


                });

                data.action = changearrytostring;
            }
        });

        if (fieldslist.length > 0) {
            var currentFromId = $("#WebFormsId").val();
            var currentFormName = $("#WebFormsName").val();

            var formsData = {
                "Id": formdata.id,
                "Name": formdata.Name,
                "EnableFallback": formdata.EnableFallback,
                "Language": formdata.Language,
                "CreatedOn": formdata.CreatedOn,
                "CreatedBy": formdata.CreatedBy,
                "ModifiedOn": formdata.ModifiedOn,
                "ModifiedBy": formdata.ModifiedBy,
                "WebFormFields": fieldslist
            }
            
            $.ajax({
                url: "/umbraco/Simpleform/api/updateform",
                type: "POST",
                data: JSON.stringify(formsData),
                contentType: "application/json",
                dataType: "json",
                success: function (response) {

                    if (response.isupdate) {
                        var iframeElementInParent = jQuery(window.frameElement, window.parent.document);
                        if (iframeElementInParent !== undefined > 0 && iframeElementInParent.parents('.form-editor-container').length > 0 && iframeElementInParent.parents('.form-editor-container').find("#backToList").length > 0) {
                            iframeElementInParent.parents('.form-editor-container').find("#backToList").click()

                        }
                        else {
                            alert('Form saved successfully');
                        }
                    }
                    else {
                        alert('Something went wrong');
                    }

                },
                error: function (response, errorText) {
                }
            });
        }
    }

    getSearchParams = function (k) {
        var p = {};
        location.search.replace(/[?&]+([^=&]+)=([^&]*)/gi, function (s, k, v) { p[k] = v })
        return k ? p[k] : p;
    }
    getfromdatafromidonpageload = function () {
        var formId = getSearchParams('id');

        $.ajax({
            url: "/umbraco/Simpleform/api/getform?id=" + formId,
            type: "GET",
            contentType: "application/x-www-form-urlencoded",
            dataType: "json",
            success: function (response) {
               
                if (response.isdata) {

                    $("#WebFormsId").val(response.data.id);
                    $("#WebFormsName").val(response.data.name);
                  
                    formdata = response.data;
                    var jsondata = JSON.stringify(GetjsonDataFromData(response.data.webFormFields));
                    var replaceandcount = jsondata.replace("[", "").replace("]", "");

                    if (replaceandcount.length > 0)
                        formhasvalue = true;
                    formbuilderDev.actions.setData(jsondata);
                }
            },
            error: function (response, errorText) {
            }
        });
    }
    getfromdatafromidonpageload();
});
