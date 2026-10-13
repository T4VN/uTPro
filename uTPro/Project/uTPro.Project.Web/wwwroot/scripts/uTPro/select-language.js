var x, i, j, l, ll, selElmnt, a, b, c;

/* look for any elements with the class "language": */
x = document.getElementsByClassName("language");
l = x.length;

for (i = 0; i < l; i++) {
    selElmnt = x[i].getElementsByTagName("select")[0];
    if (!selElmnt) continue;

    ll = selElmnt.length;

    // Selected item
    a = document.createElement("DIV");
    a.setAttribute("class", "select-selected");
    a.textContent = selElmnt.options[selElmnt.selectedIndex].textContent;
    x[i].appendChild(a);

    // Option list
    b = document.createElement("DIV");
    b.setAttribute("class", "select-items select-hide");

    for (j = 0; j < ll; j++) {
        if (selElmnt.options[j].selected) continue;

        c = document.createElement("DIV");
        c.textContent = selElmnt.options[j].textContent;

        c.addEventListener("click", function () {
            var y, i, k, s, h, sl, yl;
            s = this.parentNode.parentNode.getElementsByTagName("select")[0];
            sl = s.length;
            h = this.parentNode.previousSibling;

            for (i = 0; i < sl; i++) {
                if (s.options[i].textContent === this.textContent) {
                    s.selectedIndex = i;
                    h.textContent = this.textContent;

                    y = this.parentNode.getElementsByClassName("same-as-selected");
                    yl = y.length;
                    for (k = 0; k < yl; k++) {
                        y[k].removeAttribute("class");
                    }

                    this.setAttribute("class", "same-as-selected");
                    s.options[i].selected = true;
                    s.onchange();
                    break;
                }
            }
            h.click();
        });

        b.appendChild(c);
    }

    x[i].appendChild(b);

    a.addEventListener("click", function (e) {
        e.stopPropagation();
        closeAllSelect(this);
        this.nextSibling.classList.toggle("select-hide");
        this.classList.toggle("select-arrow-active");
    });
}

function closeAllSelect(elmnt) {
    var x, y, i, xl, yl, arrNo = [];
    x = document.getElementsByClassName("select-items");
    y = document.getElementsByClassName("select-selected");
    xl = x.length;
    yl = y.length;

    for (i = 0; i < yl; i++) {
        if (elmnt === y[i]) arrNo.push(i);
        else y[i].classList.remove("select-arrow-active");
    }

    for (i = 0; i < xl; i++) {
        if (arrNo.indexOf(i) === -1) {
            x[i].classList.add("select-hide");
        }
    }
}

document.addEventListener("click", closeAllSelect);
