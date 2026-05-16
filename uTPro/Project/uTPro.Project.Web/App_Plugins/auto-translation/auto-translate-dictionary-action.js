/**
 * uTPro Auto Translation - Workspace Action for Dictionary (Translation section)
 * 
 * When editing a dictionary item, this action translates the default language value
 * into all other language fields automatically using the API.
 */

const API_BASE = '/umbraco/api/utpro/auto-translation';

export class AutoTranslateDictionaryAction {

  host;
  args;

  constructor(host, args) {
    this.host = host;
    this.args = args;
  }

  /**
   * Called by Umbraco when the workspace action button is clicked.
   */
  async execute() {
    console.log('[AutoTranslation Dictionary] Execute called');

    try {
      // Find all text input fields (textarea, input, umb-input-textarea, etc.)
      const fields = this._findDictionaryFields();
      console.log('[AutoTranslation Dictionary] Found fields:', fields.length, fields);

      if (fields.length < 2) {
        alert('Auto Translation: Need at least 2 language fields to translate.');
        return;
      }

      // Find the source field (first non-empty one - typically the default language)
      let sourceField = null;
      let sourceIndex = -1;

      for (let i = 0; i < fields.length; i++) {
        const value = this._getFieldValue(fields[i]);
        if (value && value.trim()) {
          sourceField = fields[i];
          sourceIndex = i;
          break;
        }
      }

      if (!sourceField) {
        alert('Auto Translation: Please enter text in the default language field first.');
        return;
      }

      const sourceText = this._getFieldValue(sourceField);
      console.log('[AutoTranslation Dictionary] Source text:', sourceText);

      // Determine cultures from labels
      const cultures = this._detectCultures(fields);
      console.log('[AutoTranslation Dictionary] Detected cultures:', cultures);

      const sourceCulture = cultures[sourceIndex] || 'vi-VN';

      let translatedCount = 0;

      for (let i = 0; i < fields.length; i++) {
        if (i === sourceIndex) continue;

        const currentValue = this._getFieldValue(fields[i]);
        if (currentValue && currentValue.trim()) continue; // Skip non-empty

        const targetCulture = cultures[i] || 'en-US';

        console.log(`[AutoTranslation Dictionary] Translating "${sourceText}" to ${targetCulture}...`);

        try {
          const response = await fetch(`${API_BASE}/text`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'same-origin',
            body: JSON.stringify({
              text: sourceText,
              sourceCulture: sourceCulture,
              targetCulture: targetCulture,
              isHtml: false
            })
          });

          if (response.ok) {
            const result = await response.json();
            console.log(`[AutoTranslation Dictionary] Result:`, result);

            if (result && result.text) {
              this._setFieldValue(fields[i], result.text);
              translatedCount++;
            }
          } else {
            console.error('[AutoTranslation Dictionary] API error:', response.status);
          }
        } catch (e) {
          console.error(`[AutoTranslation Dictionary] Failed:`, e);
        }
      }

      if (translatedCount > 0) {
        alert(`✅ Auto Translation: Successfully translated ${translatedCount} language field(s)!\n\nClick Save to persist the changes.`);
      } else {
        alert('Auto Translation: No empty fields to translate into. Clear the target fields first.');
      }

    } catch (error) {
      console.error('[AutoTranslation Dictionary] Error:', error);
      alert(`Auto Translation error: ${error.message}`);
    }
  }

  /**
   * Find all dictionary translation input fields in the page.
   * Searches through Shadow DOM as Umbraco 16 uses web components.
   */
  _findDictionaryFields() {
    const fields = [];

    // Strategy 1: Find textareas directly
    const textareas = document.querySelectorAll('textarea');
    if (textareas.length >= 2) {
      return Array.from(textareas);
    }

    // Strategy 2: Search in shadow DOMs
    const allTextareas = this._querySelectorAllDeep('textarea');
    if (allTextareas.length >= 2) {
      return allTextareas;
    }

    // Strategy 3: Find umb-input-textarea or similar custom elements
    const umbInputs = this._querySelectorAllDeep('umb-input-textarea, umb-textarea, [type="textarea"]');
    if (umbInputs.length >= 2) {
      return umbInputs;
    }

    // Strategy 4: Find any input/textarea inside property editors
    const inputs = this._querySelectorAllDeep('input[type="text"], textarea, [contenteditable="true"]');
    if (inputs.length >= 2) {
      return inputs;
    }

    // Strategy 5: Fallback - find all text inputs
    return Array.from(document.querySelectorAll('input[type="text"], textarea'));
  }

  /**
   * Deep querySelector that traverses shadow DOMs.
   */
  _querySelectorAllDeep(selector) {
    const results = [];
    const traverse = (root) => {
      const found = root.querySelectorAll(selector);
      results.push(...found);

      // Traverse shadow roots
      const allElements = root.querySelectorAll('*');
      for (const el of allElements) {
        if (el.shadowRoot) {
          traverse(el.shadowRoot);
        }
      }
    };
    traverse(document);
    return results;
  }

  /**
   * Get the value from a field element.
   */
  _getFieldValue(field) {
    if (field.value !== undefined) {
      return field.value;
    }
    if (field.textContent) {
      return field.textContent;
    }
    return '';
  }

  /**
   * Set value on a field element, triggering framework reactivity.
   */
  _setFieldValue(field, value) {
    // Try native value setter for textarea/input
    const proto = field instanceof HTMLTextAreaElement
      ? HTMLTextAreaElement.prototype
      : HTMLInputElement.prototype;

    const nativeSetter = Object.getOwnPropertyDescriptor(proto, 'value')?.set;

    if (nativeSetter) {
      nativeSetter.call(field, value);
    } else {
      field.value = value;
    }

    // Dispatch events to notify the framework
    field.dispatchEvent(new Event('input', { bubbles: true, composed: true }));
    field.dispatchEvent(new Event('change', { bubbles: true, composed: true }));
    field.dispatchEvent(new Event('blur', { bubbles: true, composed: true }));
  }

  /**
   * Try to detect culture codes from field labels.
   */
  _detectCultures(fields) {
    const cultures = [];
    const cultureMap = {
      'english': 'en-US',
      'united states': 'en-US',
      'vietnamese': 'vi-VN',
      'vietnam': 'vi-VN',
      'french': 'fr-FR',
      'german': 'de-DE',
      'spanish': 'es-ES',
      'chinese': 'zh-CN',
      'japanese': 'ja-JP',
      'korean': 'ko-KR'
    };

    for (const field of fields) {
      let culture = null;

      // Try to find a label near the field
      const parent = field.closest('[class*="property"], [class*="field"], tr, .umb-property, div');
      if (parent) {
        const label = parent.querySelector('label, .label, [class*="label"]');
        if (label) {
          const labelText = label.textContent.toLowerCase();
          for (const [key, code] of Object.entries(cultureMap)) {
            if (labelText.includes(key)) {
              culture = code;
              break;
            }
          }
        }
      }

      // Fallback: check preceding sibling or parent text
      if (!culture) {
        const prevEl = field.previousElementSibling;
        if (prevEl) {
          const text = prevEl.textContent?.toLowerCase() || '';
          for (const [key, code] of Object.entries(cultureMap)) {
            if (text.includes(key)) {
              culture = code;
              break;
            }
          }
        }
      }

      cultures.push(culture);
    }

    // If we couldn't detect, use defaults based on your setup
    if (cultures.every(c => c === null)) {
      // Based on your Umbraco setup: first field = English, second = Vietnamese
      if (cultures.length >= 2) {
        cultures[0] = 'en-US';
        cultures[1] = 'vi-VN';
      }
    }

    return cultures;
  }
}

export { AutoTranslateDictionaryAction as api };
export default AutoTranslateDictionaryAction;
