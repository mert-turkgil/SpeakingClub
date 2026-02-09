<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0"
  xmlns:sitemap="http://www.sitemaps.org/schemas/sitemap/0.9"
  xmlns:xhtml="http://www.w3.org/1999/xhtml"
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  exclude-result-prefixes="sitemap xhtml">
  <xsl:output method="html" encoding="UTF-8" indent="yes"/>
  <xsl:template match="/">
    <html xmlns="http://www.w3.org/1999/xhtml">
      <head>
        <title>XML Sitemap - almanca-konus.com</title>
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
        <style type="text/css">
          body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif; color: #333; margin: 0; padding: 0; background: #f5f5f5; }
          #content { max-width: 1200px; margin: 0 auto; padding: 20px; }
          h1 { color: #1a73e8; font-size: 24px; margin-bottom: 5px; }
          p.desc { color: #666; font-size: 14px; margin-bottom: 20px; }
          table { width: 100%; border-collapse: collapse; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
          th { background: #1a73e8; color: #fff; text-align: left; padding: 12px 15px; font-size: 13px; font-weight: 600; }
          td { padding: 10px 15px; font-size: 13px; border-bottom: 1px solid #eee; }
          tr:hover td { background: #f0f7ff; }
          td a { color: #1a73e8; text-decoration: none; }
          td a:hover { text-decoration: underline; }
          .count { color: #666; font-size: 13px; margin-bottom: 15px; }
          .alt { font-size: 11px; color: #888; }
          .alt a { color: #888; font-size: 11px; }
        </style>
      </head>
      <body>
        <div id="content">
          <h1>XML Sitemap</h1>
          <p class="desc">
            This sitemap is generated for search engines. You can find more information about XML sitemaps at
            <a href="https://www.sitemaps.org/" target="_blank">sitemaps.org</a>.
          </p>
          <p class="count">
            Total URLs: <strong><xsl:value-of select="count(sitemap:urlset/sitemap:url)"/></strong>
          </p>
          <table>
            <tr>
              <th style="width:55%">URL</th>
              <th style="width:15%">Priority</th>
              <th style="width:15%">Change Freq.</th>
              <th style="width:15%">Last Modified</th>
            </tr>
            <xsl:for-each select="sitemap:urlset/sitemap:url">
              <tr>
                <td>
                  <a href="{sitemap:loc}"><xsl:value-of select="sitemap:loc"/></a>
                  <xsl:if test="xhtml:link[@hreflang]">
                    <br/>
                    <span class="alt">
                      <xsl:for-each select="xhtml:link[@hreflang]">
                        <a href="{@href}">[<xsl:value-of select="@hreflang"/>]</a>
                        <xsl:text> </xsl:text>
                      </xsl:for-each>
                    </span>
                  </xsl:if>
                </td>
                <td><xsl:value-of select="sitemap:priority"/></td>
                <td><xsl:value-of select="sitemap:changefreq"/></td>
                <td><xsl:value-of select="sitemap:lastmod"/></td>
              </tr>
            </xsl:for-each>
          </table>
        </div>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>
