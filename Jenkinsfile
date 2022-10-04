pipeline {
    agent any

    stages {
        stage('Build') {
            steps {
                echo 'Building..'
                 script {
                    sh "sudo chown -R jenkins.jenkins /var/lib/jenkins/workspace/"
                    sh "sudo dotnet build"
                }
            }
        }
        stage('Deploy') {
            steps {
                echo 'Deploying....'
                sh 'sudo chmod +x deploy-test.sh'
                
                dir('./AmiaReforged.Classes') {
                    sh 'sudo chmod +x deploy-test.sh'
                    sh './deploy-test.sh'
                }
                dir('./AmiaReforged.Races') {
                    sh 'sudo chmod +x deploy-test.sh'
                    sh './deploy-test.sh'
                }
                dir('./AmiaReforged.System') {
                    sh 'sudo chmod +x deploy-test.sh'
                    sh './deploy-test.sh'
                }
            }
        }
    }
    post {
        always {
            discordSend description: "Builder for plugin Amia.Warlock finished.", footer: "Build results for the Amia Warlock service", link: env.BUILD_URL, result: currentBuild.currentResult, title: JOB_NAME, webhookURL: "https://discord.com/api/webhooks/957814431704842270/2A6zZ4x7fsWULXnfrLLyRvgqexcnAvreXr6fbym8IoHdAHGpEoQgXjLn1XKry75uN_Zg"
            sh "sudo chown -R jenkins.jenkins /var/lib/jenkins/workspace/"
        }
        success {
            echo 'Build success'
        }
    }
}