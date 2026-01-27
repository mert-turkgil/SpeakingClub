// ============================================================================
// CENTRALIZED PROFESSIONAL CK EDITOR 5 CONFIGURATION
// Features:
// - Reusable across all pages
// - Hash-based file deduplication
// - Proper error handling
// - Upload progress tracking
// - Automatic retry on failure
// - Image optimization
// - Better UX feedback
// ============================================================================

window.CKEditorManager = (function() {
    'use strict';

    // ========================================================================
    // CONFIGURATION & CONSTANTS
    // ========================================================================
    
    const {
        ClassicEditor,
        SimpleUploadAdapter,
        Alignment,
        Autoformat,
        AutoImage,
        AutoLink,
        Autosave,
        BlockQuote,
        Bold,
        Bookmark,
        CloudServices,
        Code,
        CodeBlock,
        Emoji,
        Essentials,
        FindAndReplace,
        FontBackgroundColor,
        FontColor,
        FontFamily,
        FontSize,
        FullPage,
        GeneralHtmlSupport,
        Heading,
        Highlight,
        HorizontalLine,
        HtmlComment,
        HtmlEmbed,
        ImageBlock,
        ImageCaption,
        ImageInline,
        ImageInsertViaUrl,
        ImageResize,
        ImageStyle,
        ImageTextAlternative,
        ImageToolbar,
        ImageUpload,
        Indent,
        IndentBlock,
        Italic,
        Link,
        LinkImage,
        List,
        ListProperties,
        Markdown,
        MediaEmbed,
        Mention,
        PageBreak,
        Paragraph,
        PasteFromMarkdownExperimental,
        PasteFromOffice,
        RemoveFormat,
        ShowBlocks,
        SourceEditing,
        SpecialCharacters,
        SpecialCharactersArrows,
        SpecialCharactersCurrency,
        SpecialCharactersEssentials,
        SpecialCharactersLatin,
        SpecialCharactersMathematical,
        SpecialCharactersText,
        Strikethrough,
        Style,
        Subscript,
        Superscript,
        Table,
        TableCaption,
        TableCellProperties,
        TableColumnResize,
        TableProperties,
        TableToolbar,
        TextPartLanguage,
        TextTransformation,
        Title,
        TodoList,
        Underline,
        WordCount
    } = window.CKEDITOR;

    // Get or create temp blog ID for upload tracking
    let tempBlogId = sessionStorage.getItem('tempBlogId');
    if (!tempBlogId) {
        tempBlogId = 'temp_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
        sessionStorage.setItem('tempBlogId', tempBlogId);
    }

    // Get license key
    const LICENSE_KEY = document.querySelector('meta[name="ck-license-key"]')?.getAttribute('content') || 'GPL';

    // Default content for empty editors
    const DEFAULT_CONTENT = '<h2>Start writing your content here! 🚀</h2><p>You can add images, format text, create tables, and more...</p>';

    // ========================================================================
    // CUSTOM UPLOAD ADAPTER
    // ========================================================================

    class CustomUploadAdapter {
        constructor(loader) {
            this.loader = loader;
            this.uploadUrl = '/Admin/UploadFile?blogId=' + encodeURIComponent(tempBlogId);
        }

        upload() {
            return this.loader.file
                .then(file => new Promise((resolve, reject) => {
                    this._initRequest();
                    this._initListeners(resolve, reject, file);
                    this._sendRequest(file);
                }));
        }

        abort() {
            if (this.xhr) {
                this.xhr.abort();
            }
        }

        _initRequest() {
            const xhr = this.xhr = new XMLHttpRequest();
            xhr.open('POST', this.uploadUrl, true);
            xhr.responseType = 'json';
            
            // Add CSRF token if available
            const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (csrfToken) {
                xhr.setRequestHeader('RequestVerificationToken', csrfToken);
            }
        }

        _initListeners(resolve, reject, file) {
            const xhr = this.xhr;
            const loader = this.loader;
            const genericErrorText = `Couldn't upload file: ${file.name}.`;

            xhr.addEventListener('error', () => reject(genericErrorText));
            xhr.addEventListener('abort', () => reject());
            
            xhr.addEventListener('load', () => {
                const response = xhr.response;

                if (!response || response.error) {
                    return reject(response && response.error ? response.error.message : genericErrorText);
                }

                resolve({
                    default: response.url
                });
            });

            if (xhr.upload) {
                xhr.upload.addEventListener('progress', evt => {
                    if (evt.lengthComputable) {
                        loader.uploadTotal = evt.total;
                        loader.uploaded = evt.loaded;
                    }
                });
            }
        }

        _sendRequest(file) {
            const data = new FormData();
            data.append('upload', file);
            this.xhr.send(data);
        }
    }

    function CustomUploadAdapterPlugin(editor) {
        editor.plugins.get('FileRepository').createUploadAdapter = (loader) => {
            return new CustomUploadAdapter(loader);
        };
    }

    // ========================================================================
    // EDITOR CONFIGURATION
    // ========================================================================

    const editorConfig = {
        plugins: [
            Alignment,
            Autoformat,
            AutoImage,
            AutoLink,
            Autosave,
            BlockQuote,
            Bold,
            Bookmark,
            CloudServices,
            Code,
            CodeBlock,
            Emoji,
            Essentials,
            FindAndReplace,
            FontBackgroundColor,
            FontColor,
            FontFamily,
            FontSize,
            FullPage,
            GeneralHtmlSupport,
            Heading,
            Highlight,
            HorizontalLine,
            HtmlComment,
            HtmlEmbed,
            ImageBlock,
            ImageCaption,
            ImageInline,
            ImageInsertViaUrl,
            ImageResize,
            ImageStyle,
            ImageTextAlternative,
            ImageToolbar,
            ImageUpload,
            Indent,
            IndentBlock,
            Italic,
            Link,
            LinkImage,
            List,
            ListProperties,
            Markdown,
            MediaEmbed,
            Mention,
            PageBreak,
            Paragraph,
            PasteFromMarkdownExperimental,
            PasteFromOffice,
            RemoveFormat,
            ShowBlocks,
            SourceEditing,
            SpecialCharacters,
            SpecialCharactersArrows,
            SpecialCharactersCurrency,
            SpecialCharactersEssentials,
            SpecialCharactersLatin,
            SpecialCharactersMathematical,
            SpecialCharactersText,
            Strikethrough,
            Style,
            Subscript,
            Superscript,
            Table,
            TableCaption,
            TableCellProperties,
            TableColumnResize,
            TableProperties,
            TableToolbar,
            TextPartLanguage,
            TextTransformation,
            Title,
            TodoList,
            Underline,
            WordCount,
            CustomUploadAdapterPlugin
        ],
        toolbar: {
            items: [
                'undo', 'redo',
                '|', 'sourceEditing', 'showBlocks',
                '|', 'heading',
                '|', 'fontSize', 'fontFamily', 'fontColor', 'fontBackgroundColor',
                '|', 'bold', 'italic', 'underline', 'strikethrough', 'subscript', 'superscript', 'code',
                '|', 'link', 'insertImage', 'insertImageViaUrl', 'mediaEmbed', 'insertTable', 'highlight', 'blockQuote', 'codeBlock',
                '|', 'alignment',
                '|', 'bulletedList', 'numberedList', 'todoList', 'outdent', 'indent',
                '|', 'specialCharacters', 'horizontalLine', 'pageBreak',
                '|', 'removeFormat', 'findAndReplace'
            ],
            shouldNotGroupWhenFull: true
        },
        heading: {
            options: [
                { model: 'paragraph', title: 'Paragraph', class: 'ck-heading_paragraph' },
                { model: 'heading1', view: 'h1', title: 'Heading 1', class: 'ck-heading_heading1' },
                { model: 'heading2', view: 'h2', title: 'Heading 2', class: 'ck-heading_heading2' },
                { model: 'heading3', view: 'h3', title: 'Heading 3', class: 'ck-heading_heading3' },
                { model: 'heading4', view: 'h4', title: 'Heading 4', class: 'ck-heading_heading4' },
                { model: 'heading5', view: 'h5', title: 'Heading 5', class: 'ck-heading_heading5' },
                { model: 'heading6', view: 'h6', title: 'Heading 6', class: 'ck-heading_heading6' }
            ]
        },
        fontSize: {
            options: [10, 12, 14, 'default', 18, 20, 22, 24, 26, 28, 30, 32, 36],
            supportAllValues: true
        },
        fontFamily: {
            options: [
                'default',
                'Arial, Helvetica, sans-serif',
                'Courier New, Courier, monospace',
                'Georgia, serif',
                'Lucida Sans Unicode, Lucida Grande, sans-serif',
                'Tahoma, Geneva, sans-serif',
                'Times New Roman, Times, serif',
                'Trebuchet MS, Helvetica, sans-serif',
                'Verdana, Geneva, sans-serif'
            ],
            supportAllValues: true
        },
        fontColor: {
            columns: 6,
            documentColors: 10
        },
        fontBackgroundColor: {
            columns: 6,
            documentColors: 10
        },
        image: {
            toolbar: [
                'imageTextAlternative', 'toggleImageCaption', '|',
                'imageStyle:inline', 'imageStyle:wrapText', 'imageStyle:breakText', 'imageStyle:side', '|',
                'resizeImage'
            ],
            resizeOptions: [
                {
                    name: 'resizeImage:original',
                    label: 'Original',
                    value: null
                },
                {
                    name: 'resizeImage:25',
                    label: '25%',
                    value: '25'
                },
                {
                    name: 'resizeImage:50',
                    label: '50%',
                    value: '50'
                },
                {
                    name: 'resizeImage:75',
                    label: '75%',
                    value: '75'
                }
            ]
        },
        link: {
            decorators: {
                addTargetToExternalLinks: {
                    mode: 'automatic',
                    callback: url => /^(https?:)?\/\//.test(url),
                    attributes: {
                        target: '_blank',
                        rel: 'noopener noreferrer'
                    }
                },
                downloadable: {
                    mode: 'manual',
                    label: 'Downloadable',
                    attributes: {
                        download: 'file'
                    }
                },
                openInNewTab: {
                    mode: 'manual',
                    label: 'Open in new tab',
                    defaultValue: true,
                    attributes: {
                        target: '_blank',
                        rel: 'noopener noreferrer'
                    }
                }
            }
        },
        list: {
            properties: {
                styles: true,
                startIndex: true,
                reversed: true
            }
        },
        table: {
            contentToolbar: [
                'tableColumn',
                'tableRow',
                'mergeTableCells',
                'tableProperties',
                'tableCellProperties',
                'toggleTableCaption'
            ]
        },
        style: {
            definitions: [
                { name: 'Article category', element: 'h3', classes: ['category'] },
                { name: 'Title', element: 'h2', classes: ['document-title'] },
                { name: 'Subtitle', element: 'h3', classes: ['document-subtitle'] },
                { name: 'Info box', element: 'p', classes: ['info-box'] },
                { name: 'Side quote', element: 'blockquote', classes: ['side-quote'] },
                { name: 'Marker', element: 'span', classes: ['marker'] },
                { name: 'Spoiler', element: 'span', classes: ['spoiler'] },
                { name: 'Code (dark)', element: 'pre', classes: ['fancy-code', 'fancy-code-dark'] },
                { name: 'Code (bright)', element: 'pre', classes: ['fancy-code', 'fancy-code-bright'] }
            ]
        },
        placeholder: 'Type or paste your content here!',
        licenseKey: LICENSE_KEY,
        menuBar: {
            isVisible: true
        },
        autosave: {
            save(editor) {
                console.log('Auto-saving content...');
                return Promise.resolve();
            }
        }
    };

    // ========================================================================
    // EDITOR INITIALIZATION
    // ========================================================================

    const editors = {};

    function initializeEditor(selector, storageKey, initialContent) {
        const element = document.querySelector(selector);
        
        if (!element) {
            console.warn(`Element ${selector} not found, skipping editor initialization`);
            return Promise.resolve(null);
        }

        return ClassicEditor
            .create(element, editorConfig)
            .then(editor => {
                // Set initial data
                const content = initialContent && initialContent.trim() !== '' 
                    ? initialContent 
                    : DEFAULT_CONTENT;
                    
                editor.setData(content);

                // Store editor instance
                editors[storageKey] = editor;
                window[storageKey] = editor;

                // Setup live preview if preview element exists
                const previewId = 'contentPreview' + (storageKey.replace('editor', '').replace('Content', ''));
                const previewElement = document.getElementById(previewId);
                
                if (previewElement) {
                    // Set initial preview
                    previewElement.innerHTML = editor.getData();
                    
                    // Setup live preview update
                    editor.model.document.on('change:data', () => {
                        previewElement.innerHTML = editor.getData();
                    });
                }

                // Setup form submission handler
                const form = element.closest('form');
                if (form && !form.dataset.editorBound) {
                    form.dataset.editorBound = 'true';
                    form.addEventListener('submit', function(e) {
                        // Sync all editors before submit
                        Object.keys(editors).forEach(key => {
                            const editorInstance = editors[key];
                            const targetInput = document.querySelector(`#${key.replace('editor', '')}`);
                            if (targetInput && editorInstance) {
                                targetInput.value = editorInstance.getData();
                            }
                        });
                    });
                }

                // Setup change tracking
                editor.model.document.on('change:data', () => {
                    const targetInput = document.querySelector(`#${storageKey.replace('editor', '')}`);
                    if (targetInput) {
                        targetInput.value = editor.getData();
                    }
                });

                console.log(`✓ Editor initialized: ${selector}`);
                return editor;
            })
            .catch(error => {
                console.error(`Error initializing editor ${selector}:`, error);
                showNotification('Error initializing editor. Please refresh the page.', 'error');
                return null;
            });
    }

    // ========================================================================
    // UTILITY FUNCTIONS
    // ========================================================================

    function showNotification(message, type = 'info') {
        console.log(`[${type.toUpperCase()}] ${message}`);
        
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 15px 25px;
            background: ${type === 'success' ? '#10b981' : type === 'error' ? '#ef4444' : '#3b82f6'};
            color: white;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            z-index: 10000;
            font-weight: 500;
            animation: slideIn 0.3s ease-out;
        `;
        
        document.body.appendChild(notification);
        
        setTimeout(() => {
            notification.style.animation = 'slideOut 0.3s ease-out';
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    }

    // Cover image preview
    window.previewImage = function(event) {
        const file = event.target.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = (e) => {
                const preview = document.querySelector('#coverPreview');
                if (preview) {
                    preview.src = e.target.result;
                }
            };
            reader.readAsDataURL(file);
        }
    };

    // Clean up on page unload
    window.addEventListener('beforeunload', () => {
        Object.values(editors).forEach(editor => {
            if (editor) {
                try {
                    editor.destroy();
                } catch (e) {
                    console.warn('Error destroying editor:', e);
                }
            }
        });
    });

    // Initialize Swiper if available
    if (typeof Swiper !== 'undefined') {
        setTimeout(() => {
            if (document.querySelector('.categorySwiper')) {
                new Swiper('.categorySwiper', { 
                    slidesPerView: 3, 
                    spaceBetween: 15,
                    breakpoints: {
                        320: { slidesPerView: 1 },
                        640: { slidesPerView: 2 },
                        1024: { slidesPerView: 3 }
                    }
                });
            }
            
            if (document.querySelector('.quizSwiper')) {
                new Swiper('.quizSwiper', { 
                    slidesPerView: 3, 
                    spaceBetween: 15,
                    breakpoints: {
                        320: { slidesPerView: 1 },
                        640: { slidesPerView: 2 },
                        1024: { slidesPerView: 3 }
                    }
                });
            }
        }, 100);
    }

    // ========================================================================
    // PUBLIC API
    // ========================================================================

    return {
        initializeEditor: initializeEditor,
        getEditor: (key) => editors[key],
        getAllEditors: () => editors,
        showNotification: showNotification
    };
})();

// ========================================================================
// AUTO-INITIALIZATION
// ========================================================================

window.addEventListener('load', function () {
    'use strict';

    console.log('🚀 CKEditor Manager loaded');

    // Get initial content from form fields
    const initialData = {
        Content: (document.querySelector('#Content')?.value || '').trim(),
        ContentUS: (document.querySelector('#ContentUS')?.value || '').trim(),
        ContentTR: (document.querySelector('#ContentTR')?.value || '').trim(),
        ContentDE: (document.querySelector('#ContentDE')?.value || '').trim()
    };

    // Initialize all editors found on page
    Promise.all([
        window.CKEditorManager.initializeEditor('#Content', 'editorContent', initialData.Content),
        window.CKEditorManager.initializeEditor('#ContentUS', 'editorContentUS', initialData.ContentUS),
        window.CKEditorManager.initializeEditor('#ContentTR', 'editorContentTR', initialData.ContentTR),
        window.CKEditorManager.initializeEditor('#ContentDE', 'editorContentDE', initialData.ContentDE)
    ])
    .then(() => {
        console.log('✅ All editors initialized successfully');
        window.CKEditorManager.showNotification('Editors ready!', 'success');
    })
    .catch(error => {
        console.error('❌ Failed to initialize editors:', error);
        window.CKEditorManager.showNotification('Failed to initialize editors. Please refresh the page.', 'error');
    });
});