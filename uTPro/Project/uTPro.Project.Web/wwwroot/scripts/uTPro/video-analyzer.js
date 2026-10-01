/**
 * uTPro Video Analyzer — front-end logic for the pageVideoAnalyzer template.
 * Vanilla JS (no jQuery): form submit → poll job → render report → chat.
 * Talks to /api/video-analyzer/* which requires a signed-in member.
 */
(function () {
    'use strict';

    var el = function (id) { return document.getElementById(id); };

    var form = el('va-form'), urlInput = el('va-url'), langSelect = el('va-lang'),
        submitBtn = el('va-submit'), quotaNote = el('va-quota'),
        status = el('va-status'), progressBar = el('va-progress-bar'), stageLabel = el('va-stage'),
        errorBox = el('va-error'), report = el('va-report'), chatSection = el('va-chat'),
        chatLog = el('va-chat-log'), chatForm = el('va-chat-form'), chatInput = el('va-chat-input');

    var currentAnalysisKey = null;
    var pollTimer = null;

    var STAGES = {
        queued: 'Đang xếp hàng…',
        metadata: 'Đang lấy thông tin video…',
        transcript: 'Đang lấy phụ đề…',
        analysis: 'AI đang xem và phân tích video…',
        saving: 'Đang lưu kết quả…',
        done: 'Hoàn tất!'
    };

    var AGE_STYLES = {
        P: 'va-age-p', K: 'va-age-k', C13: 'va-age-c13', C16: 'va-age-c16', C18: 'va-age-c18'
    };

    function fmtTime(seconds) {
        var s = Math.max(0, Math.floor(seconds || 0));
        var h = Math.floor(s / 3600), m = Math.floor((s % 3600) / 60), sec = s % 60;
        var mm = (h > 0 ? String(m).padStart(2, '0') : String(m));
        return (h > 0 ? h + ':' + mm : mm) + ':' + String(sec).padStart(2, '0');
    }

    function fmtDuration(seconds) {
        if (!seconds) { return ''; }
        var s = Math.floor(seconds);
        var h = Math.floor(s / 3600), m = Math.floor((s % 3600) / 60), sec = s % 60;
        return h > 0
            ? h + 'h' + String(m).padStart(2, '0') + 'm'
            : m + ':' + String(sec).padStart(2, '0');
    }

    function fmtDate(iso) {
        if (!iso) { return ''; }
        try { return new Date(iso).toLocaleString('vi-VN'); } catch (e) { return iso; }
    }

    function showError(message) {
        errorBox.textContent = message || 'Có lỗi không xác định.';
        errorBox.hidden = false;
    }

    function clearError() {
        errorBox.hidden = true;
        errorBox.textContent = '';
    }

    function setBusy(busy) {
        submitBtn.disabled = busy;
        submitBtn.textContent = busy ? 'Đang xử lý…' : 'Phân tích';
    }

    function setProgress(percent, stage) {
        status.hidden = false;
        progressBar.style.width = Math.min(100, Math.max(4, percent || 4)) + '%';
        stageLabel.textContent = STAGES[stage] || stageLabel.textContent;
    }

    function setQuota(remaining) {
        if (remaining === null || remaining === undefined) {
            quotaNote.hidden = true;
            return;
        }
        quotaNote.textContent = 'Số lần phân tích mới còn lại hôm nay: ' + remaining;
        quotaNote.hidden = false;
    }

    function fillList(listEl, items, cssClass) {
        listEl.innerHTML = '';
        (items || []).forEach(function (item) {
            var li = document.createElement('li');
            li.textContent = item;
            if (cssClass) { li.className = cssClass; }
            listEl.appendChild(li);
        });
        listEl.parentElement.hidden = (items || []).length === 0;
    }

    function renderReport(data) {
        var r = data.report;
        report.hidden = false;
        chatSection.hidden = false;
        status.hidden = true;

        el('va-thumb').src = data.thumbnailUrl || '';
        el('va-thumb').hidden = !data.thumbnailUrl;
        el('va-title').textContent = data.title || data.sourceUrl;

        var metaBits = [];
        if (data.channelTitle) { metaBits.push(data.channelTitle); }
        if (data.durationSeconds) { metaBits.push(fmtDuration(data.durationSeconds)); }
        if (data.completedAt) { metaBits.push('Phân tích: ' + fmtDate(data.completedAt)); }
        if (data.cacheHits > 0) { metaBits.push('Đã dùng lại ' + data.cacheHits + ' lần'); }
        el('va-meta').textContent = metaBits.join(' · ');

        el('va-summary').textContent = (r && r.summary) || '—';

        var age = (r && r.ageRating) || {};
        var ageLabel = el('va-age-label');
        ageLabel.textContent = age.label || '?';
        ageLabel.className = 'va-age-label ' + (AGE_STYLES[age.label] || '');
        fillList(el('va-age-flags'), age.flags, 'va-flag-chip');
        fillList(el('va-age-reasons'), age.reasons);
        el('va-age-confidence').textContent =
            'Độ tin cậy: ' + Math.round((age.confidence || 0) * 100) + '% (gợi ý bởi AI)';

        fillList(el('va-topics'), r && r.topics, 'va-chip');
        fillList(el('va-keywords'), r && r.keywords, 'va-chip');

        var mentionsEl = el('va-mentions');
        mentionsEl.innerHTML = '';
        ((r && r.mentions) || []).forEach(function (mention) {
            var li = document.createElement('li');
            li.className = 'va-mention';
            li.innerHTML = '<strong></strong><span class="va-chip va-mention-type"></span><span class="va-mention-ctx"></span>';
            li.querySelector('strong').textContent = mention.name;
            li.querySelector('.va-mention-type').textContent = mention.type || '';
            li.querySelector('.va-mention-ctx').textContent = mention.context || '';
            mentionsEl.appendChild(li);
        });
        mentionsEl.parentElement.hidden = mentionsEl.childElementCount === 0;

        var timelineEl = el('va-timeline');
        timelineEl.innerHTML = '';
        ((r && r.timeline) || []).forEach(function (chapter) {
            var li = document.createElement('li');
            li.innerHTML = '<span class="va-time"></span><div><strong class="va-tl-title"></strong><p class="va-tl-desc"></p></div>';
            li.querySelector('.va-time').textContent = fmtTime(chapter.startSeconds);
            li.querySelector('.va-tl-title').textContent = chapter.title || '';
            li.querySelector('.va-tl-desc').textContent = chapter.description || '';
            timelineEl.appendChild(li);
        });
        timelineEl.parentElement.parentElement.hidden = timelineEl.childElementCount === 0;

        el('va-sentiment').textContent = (r && r.sentiment) ? 'Tông nội dung: ' + r.sentiment : '';
        var sourceBits = [];
        if (r && r.contentSource === 'ai-video') { sourceBits.push('Nội dung: AI xem trực tiếp video'); }
        if (r && r.contentSource === 'transcript') { sourceBits.push('Nội dung: phân tích từ phụ đề'); }
        if (r && r.modelName) { sourceBits.push(r.modelName); }
        if (data.analysisVersion > 1) { sourceBits.push('Phiên bản #' + data.analysisVersion); }
        el('va-source').textContent = sourceBits.join(' · ');

        chatLog.innerHTML = '';
    }

    function analyzeAgain() {
        if (!currentAnalysisKey) { return; }
        clearError();
        setBusy(true);
        report.hidden = true;
        chatSection.hidden = true;
        setProgress(4, 'queued');
        fetch('/api/video-analyzer/reports/' + currentAnalysisKey + '/refresh', { method: 'POST' })
            .then(function (res) { return handleStatus(res); })
            .then(function (data) {
                setQuota(data.quotaRemaining);
                startPolling(data.analysisKey);
            })
            .catch(function (err) { setBusy(false); status.hidden = true; showError(err && err.message); });
    }

    function handleStatus(res) {
        if (res.status === 401) {
            return Promise.reject(new Error('Bạn cần đăng nhập để sử dụng chức năng này.'));
        }
        if (res.status === 403) {
            return Promise.reject(new Error('Tài khoản của bạn chưa có quyền truy cập. Vui lòng liên hệ quản trị viên.'));
        }
        if (res.status === 429) {
            return res.json().catch(function () { return {}; }).then(function (body) {
                throw new Error(body.error || 'Bạn đã hết lượt phân tích trong hôm nay.');
            });
        }
        if (!res.ok) {
            return res.json().catch(function () { return {}; }).then(function (body) {
                throw new Error(body.error || ('Yêu cầu thất bại (' + res.status + ').'));
            });
        }
        return res.json();
    }

    function startPolling(key) {
        currentAnalysisKey = key;
        if (pollTimer) { window.clearInterval(pollTimer); }
        pollTimer = window.setInterval(function () {
            fetch('/api/video-analyzer/jobs/' + key)
                .then(function (res) { return handleStatus(res); })
                .then(function (job) {
                    if (job.status === 'Success') {
                        window.clearInterval(pollTimer);
                        return fetchReport(key);
                    }
                    if (job.status === 'Failed') {
                        window.clearInterval(pollTimer);
                        status.hidden = true;
                        setBusy(false);
                        showError(job.error || 'Phân tích thất bại.');
                        return null;
                    }
                    setProgress(job.progress || 4, job.stage);
                    return null;
                })
                .catch(function (err) {
                    window.clearInterval(pollTimer);
                    status.hidden = true;
                    setBusy(false);
                    showError(err && err.message);
                });
        }, 2000);
    }

    function fetchReport(key) {
        return fetch('/api/video-analyzer/reports/' + key)
            .then(function (res) { return handleStatus(res); })
            .then(function (data) {
                setBusy(false);
                renderReport(data);
            })
            .catch(function (err) {
                status.hidden = true;
                setBusy(false);
                showError(err && err.message);
            });
    }

    function submitAnalysis(event) {
        event.preventDefault();
        clearError();
        report.hidden = true;
        chatSection.hidden = true;
        setBusy(true);
        setProgress(4, 'queued');

        fetch('/api/video-analyzer/analyze', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ url: urlInput.value.trim(), language: langSelect.value })
        })
            .then(function (res) { return handleStatus(res); })
            .then(function (data) {
                setQuota(data.quotaRemaining);
                if (data.message) { console.info(data.message); }
                if (data.cacheHit) {
                    status.hidden = true;
                    setBusy(false);
                    currentAnalysisKey = data.analysisKey;
                    return fetchReport(data.analysisKey);
                }
                startPolling(data.analysisKey);
                return null;
            })
            .catch(function (err) {
                status.hidden = true;
                setBusy(false);
                showError(err && err.message);
            });
    }

    function appendChatBubble(role, text) {
        var bubble = document.createElement('div');
        bubble.className = 'va-bubble va-bubble-' + (role === 'user' ? 'user' : 'bot');
        bubble.textContent = text;
        chatLog.appendChild(bubble);
        chatLog.scrollTop = chatLog.scrollHeight;
        return bubble;
    }

    function submitChat(event) {
        event.preventDefault();
        var message = chatInput.value.trim();
        if (!message || !currentAnalysisKey) { return; }
        chatInput.value = '';
        appendChatBubble('user', message);
        var pending = appendChatBubble('bot', '…');

        fetch('/api/video-analyzer/reports/' + currentAnalysisKey + '/chat', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message: message })
        })
            .then(function (res) { return handleStatus(res); })
            .then(function (data) {
                pending.textContent = data.reply;
            })
            .catch(function (err) {
                pending.remove();
                appendChatBubble('bot', (err && err.message) || 'Không gửi được câu hỏi.');
            });
    }

    form.addEventListener('submit', submitAnalysis);
    el('va-refresh').addEventListener('click', analyzeAgain);
    chatForm.addEventListener('submit', submitChat);
})();
