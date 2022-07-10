<?xml version="1.0"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:frmwrk="Corel Framework Data">
  <xsl:output method="xml" encoding="UTF-8" indent="yes"/>

  <frmwrk:uiconfig>
   
    <frmwrk:applicationInfo userConfiguration="true" />
  </frmwrk:uiconfig>

  <!-- Copy everything -->
  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="uiConfig/items">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
		<!-- Define the button will contains menu is same in all projects -->
		<itemData guid="f1d3d1d0-cc8d-4f04-91cb-7112255b8af1" noBmpOnMenu="true"
				  type="flyout"
				  dynamicCategory="2cc24a3e-fe24-4708-9a74-9c75406eebcd"
				  userCaption="Bonus630 Dockers"
				  enable="true"
				  flyoutBarRef="FB727225-CEA7-4D27-BB27-52C687B53029"
                />
      <!-- Define the button which shows the docker -->
      <itemData guid="657f1d20-df86-443f-931f-be57f9537621" noBmpOnMenu="true"
                type="checkButton"
                check="*Docker('5087687d-337d-4d0e-acaf-c0b1df967757')"
                dynamicCategory="2cc24a3e-fe24-4708-9a74-9c75406eebcd"
                userCaption="Run Command"
                enable="true"/>

      <!-- Define the web control which will be placed on our docker -->
      <itemData guid="2ee3372b-f6b5-47fe-aa81-4ecd2c4771e2"
                type="wpfhost"
                hostedType="Addons\RunCommandDocker\RunCommandDocker.dll,RunCommandDocker.DockerUI"
                enable="true"/>

    </xsl:copy>
  </xsl:template>
	<!-- Define the new menu is same in all others project-->
	<xsl:template match="uiConfig/commandBars">
		<xsl:copy>
			<xsl:apply-templates select="node()|@*"/>

			<commandBarData guid="FB727225-CEA7-4D27-BB27-52C687B53029"
							type="menu"
							nonLocalizableName="Bonus630 Dockers"
							flyout="true">
				<menu>

					<!--Here change to new item-->
					<!--<item guidRef="DF67BEBE-6551-4F3B-BE5B-1BF46E16AB67"/>-->

				</menu>
			</commandBarData>
		</xsl:copy>
	</xsl:template>
	<xsl:template match="uiConfig/commandBars/commandBarData[guid='FB727225-CEA7-4D27-BB27-52C687B53029']/menu">
		<xsl:copy>
			<xsl:apply-templates select="node()|@*"/>

					<!--Here change to new item-->
					<item guidRef="657f1d20-df86-443f-931f-be57f9537621"/>

		</xsl:copy>
	</xsl:template>
  <xsl:template match="uiConfig/dockers">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>

      <!-- Define the web docker -->
      <dockerData guid="5087687d-337d-4d0e-acaf-c0b1df967757"
                  userCaption="Run Command"
                  wantReturn="true"
                  focusStyle="noThrow">
        <container>
          <!-- add the webpage control to the docker -->
          <item dock="fill" margin="0,0,0,0" guidRef="2ee3372b-f6b5-47fe-aa81-4ecd2c4771e2"/>
        </container>
      </dockerData>
    </xsl:copy>
  </xsl:template>

</xsl:stylesheet>
